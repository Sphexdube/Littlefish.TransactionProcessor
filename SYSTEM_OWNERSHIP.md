# System Ownership

This document addresses production ownership concerns: scaling strategy, exactly-once processing guarantees, required dashboards and alerts, and known technical debt. It is written from the perspective of a long-term system owner, not a task implementer.

---

## Scaling to 100 Million Transactions Per Day

### Throughput Baseline

100 M tx/day ≈ **1,157 tx/second** average. Realistic Tier 1 bank intraday peaks of 3–5× that gives **~5,000 TPS** at peak load.

### Current Architecture (as implemented)

```
API (n instances)                     Azure Service Bus
POST /transactions:ingest             queue: "transactions-ingest"
        │                                      │
        │ INSERT TransactionRecord             │ subscribe (MaxConcurrentCalls=4)
        │ INSERT OutboxMessage (atomic)        │
        ▼                                      ▼
    SQL Server ◄──── OutboxRelayWorker ──►  Processor Worker
                     polls every 1s          evaluates rules
                     batch 100               updates status + MerchantDailySummary
```

Three separate services:
- **`Transaction.Presentation.Api`** — ingests batches, writes outbox, returns `202` immediately
- **`Transaction.Worker.OutboxRelay`** — polls `OutboxMessages`, publishes to ASB, marks published
- **`Transaction.Worker.Processor`** — subscribes to ASB, evaluates rules, commits final status

### What Breaks First, In Order

#### 1. The `OutboxMessages` table at high write rates

The relay worker polls `WHERE Published = false ORDER BY CreatedAt TAKE 100` every 1 second. At 5,000 TPS ingestion:
- The table accumulates 5,000 unpublished rows per second.
- The polling index scan (`Published`, `CreatedAt`) becomes a hot range — lock contention grows with relay lag.
- Multiple relay instances will compete on the same unpublished rows without a partitioning strategy.

**Fix:** Add a relay-instance claim step (similar to the processing worker's use of ASB message locking): set `RelayInstanceId` and `ClaimedAt` before publishing, so multiple relay instances can shard the outbox without row contention. Alternatively, partition the outbox by `TenantId` and assign one relay instance per tenant shard.

#### 2. The `MerchantDailySummary` hot row

For a popular merchant, every processed PURCHASE updates the same row. At 5,000 TPS with 10% being PURCHASE transactions for one merchant, that row receives 500 writes/second. The `rowversion` retry loop handles 2–3 concurrent conflicts cleanly, but at hundreds of concurrent writers it becomes a serialisation point and retry storm.

**Fix:** Accumulate deltas in memory per worker over a short window (e.g. 500ms), then apply one batched `UPDATE MerchantDailySummary SET TotalAmount += @delta, TransactionCount += @count WHERE ...` per merchant per window. Alternatively, use Redis `INCRBYFLOAT` as a fast counter guard and reconcile with the DB counter asynchronously. Either approach reduces the hot-row update rate by orders of magnitude.

#### 3. The SQL Server write path

At 5,000 TPS:
- `Transactions` table receives ~5,000 inserts/sec from the API.
- `OutboxMessages` receives ~5,000 inserts/sec.
- Each processed transaction triggers an additional update from the worker.
- Index maintenance on `(TenantId, TransactionId)`, `(TenantId, MerchantId, Date)` adds overhead.
- A single SQL Server instance's transaction log becomes the throughput ceiling.

**Fix:**
- Read replicas (SQL Server AlwaysOn AG) for status query endpoints — `/transactions/{id}` and `/daily-summary`.
- Partition `Transactions` by `TenantId` to distribute index hot-spots.
- Consider an append-only ledger model for ingestion (write once, project status from events) — eliminates in-place status updates.

#### 4. Azure Service Bus throughput ceiling

The standard ASB tier supports ~1,000 messages/sec per queue. At 5,000 TPS ingestion, the relay publishing rate will saturate a standard-tier queue.

**Fix:** Upgrade to ASB Premium tier (supports partitioned entities and much higher throughput). Partition the queue by `TenantId` using ASB sessions — each worker instance owns a session and processes one tenant's messages sequentially, eliminating the need for `rowversion` retries within a single session.

#### 5. The API ingestion path

ASP.NET Core can handle high request rates, but:
- Parsing and validating 5,000-item JSON arrays synchronously per request applies thread pressure.
- The `ExistsByTransactionIdAsync` duplicate check is O(N) per batch — 5,000 items → 5,000 individual DB reads.

**Fix:** Horizontal API scale-out behind a load balancer (AKS). Replace per-item duplicate check with a bulk `WHERE TransactionId IN (...)` query. For very large batches, consider streaming JSON parsing (`System.Text.Json` `Utf8JsonReader`).

### Scale-Out Architecture Target

```
                     ┌──────────────────────────────────────┐
                     │         API (n AKS pods)              │
                     │  POST /transactions/:ingest           │
                     │  Bulk duplicate check                 │
                     └─────────────────┬────────────────────┘
                                       │ INSERT Transactions + OutboxMessages (atomic)
                                       ▼
                     ┌──────────────────────────────────────┐
                     │         SQL Server (primary)          │
                     │  + read replicas for GET endpoints   │
                     │  + partitioned by TenantId           │
                     └────────┬─────────────────────────────┘
                              │ relay (sharded by TenantId)
                     ┌────────▼─────────────────────────────┐
                     │     OutboxRelayWorker (n instances)  │
                     │  One shard per relay instance        │
                     └────────┬─────────────────────────────┘
                              │ publish (ASB Premium, partitioned)
                     ┌────────▼─────────────────────────────┐
                     │   Azure Service Bus Premium           │
                     │   Partitioned queue by TenantId      │
                     │   Sessions = TenantId                │
                     └────────┬─────────────────────────────┘
                              │ consume (one session per worker)
                     ┌────────▼─────────────────────────────┐
                     │  TransactionProcessingWorker (n pods) │
                     │  Each pod owns 1–N tenant sessions   │
                     │  Batched MerchantDailySummary updates │
                     └──────────────────────────────────────┘
```

---

## Exactly-Once vs At-Least-Once Strategy

### Guarantee Provided

The current system provides **at-least-once delivery with idempotent processing** — which is the correct target for a financial transaction system. True exactly-once requires distributed transactions across heterogeneous systems (DB + message broker), which is impractical and rarely necessary when idempotency is correctly applied.

### How It Works End-to-End

**Ingestion (API → DB):**
- `(TenantId, TransactionId)` unique index — duplicate inserts are rejected at the DB level.
- `ExistsByTransactionIdAsync` application-level guard returns a friendly rejection rather than letting the exception propagate.
- Result: A re-submitted batch with the same `transactionId` is safe — it will be returned in `errors[]` and not double-persisted.

**Relay (DB → ASB):**
- Outbox pattern: `TransactionRecord` and `OutboxMessage` are written in a single `SaveChangesAsync` call. If the API crashes after DB commit but before relay, the unpublished message is picked up on the next relay poll cycle.
- The relay marks `Published = true` only after `PublishAsync` returns successfully. If the relay crashes after publishing but before marking published, the message is re-published on the next cycle — but the processor handles this idempotently (see below).

**Processing (ASB → DB):**
- `AutoCompleteMessages = false` — ASB only removes the message from the queue after the processor explicitly calls `CompleteMessageAsync`.
- If the processor crashes after the DB commit but before `CompleteMessageAsync`, ASB redelivers the message. The processor checks `transaction.Status != Received` at the start — already-processed transactions are acknowledged without re-evaluation.
- `rowversion` concurrency guard prevents two worker instances from committing conflicting status updates.
- Dead-letter: messages that cannot be processed (deserialisation failure, missing tenant, unexpected error) are moved to the dead-letter queue rather than retried indefinitely.

### Remaining Gap — "In-flight" Crash Window

If the processor sets `Status = Processing` and the DB transaction commits, then the worker crashes before calling `CompleteMessageAsync`, ASB will redeliver the message. The processor will find `Status = Processing` (not `Received`) and complete the message without re-evaluating. This is correct behaviour **only if** the DB transaction also committed the final status. If the DB commit was not yet reached (crash between `BeginTransactionAsync` and `CommitTransactionAsync`), the final status is not persisted and the transaction is left in `Processing` indefinitely.

**Mitigation:** Add a watchdog check that resets transactions stuck in `Processing` for more than a configurable timeout (e.g. 5 minutes) back to `Received`. This allows re-delivery from the outbox if the ASB message was already dead-lettered, or re-insertion of a new outbox message.

---

## Dashboards and Alerts

### Key Metrics (Actual Instrument Names)

All custom metrics are emitted under `Meter("Littlefish.TransactionProcessor")` and exported via OTLP.

| Instrument | Kind | Description | Alert Threshold |
|---|---|---|---|
| `transactions.ingested` | Counter | Accepted into a batch by the API | — |
| `transactions.processed` | Counter | Successfully completed by the worker | — |
| `transactions.rejected` | Counter | Rejected by rule or validation | Spike > 5× 1h baseline |
| `transactions.in_review` | Counter | Flagged for manual review | — |
| `batches.received` | Counter | Batches accepted by the API | — |
| `batches.completed` | Counter | Batches fully processed | — |
| `daily_limit.checks` | Counter | Daily limit rule evaluations | — |
| `daily_limit.exceeded` | Counter | Daily limit rejections | Spike > 2× 1h baseline |
| `outbox.messages_relayed` | Counter | Messages successfully published to ASB | — |
| `outbox.relay_errors` | Counter | Unexpected errors in the relay loop | > 0 for 2m |
| `servicebus.dead_lettered` | Counter | Messages sent to the ASB dead-letter queue | > 0 for 1m |
| `servicebus.abandoned` | Counter | Messages abandoned for ASB redelivery | > 10/min |
| `transactions.concurrency_conflicts` | Counter | `DbUpdateConcurrencyException` events | > 50/min |
| `transactions.processing_duration` | Histogram (ms) | End-to-end processing time per message | p99 > 5,000ms |

**Additional platform metrics to collect (via ASP.NET Core / ASB SDK instrumentation):**

| Metric | Source | Alert |
|---|---|---|
| `http.server.request.duration` (p99) | ASP.NET Core | > 500ms |
| `http.server.active_requests` | ASP.NET Core | > 200 |
| `servicebus.receiver.message_count` (queue depth) | ASB namespace metrics | > 50,000 |
| `servicebus.receiver.dead_letter_count` | ASB namespace metrics | > 100 for 5m |
| SQL Server CPU / IO | Azure Monitor / SQL Insights | CPU > 80% for 5m |

### Recommended Dashboard Layout (Grafana / Azure Monitor)

**Row 1 — Throughput**
- Ingestion rate (`transactions.ingested` rate/min) vs processing rate (`transactions.processed` rate/min)
- Rejection rate (`transactions.rejected / transactions.ingested`, %)
- Batch rate (`batches.received` rate/min)

**Row 2 — Latency**
- `transactions.processing_duration` p50 / p95 / p99 (histogram buckets)
- API handler duration p50 / p95 / p99 (`http.server.request.duration`)

**Row 3 — Queue Health**
- ASB queue depth (total + dead-letter) — trend + current value
- `outbox.messages_relayed` vs `transactions.ingested` — lag indicator
- `servicebus.abandoned` rate

**Row 4 — Business Rules**
- Rejection breakdown by rule (`transactions.rejected` labelled by context — requires enrichment)
- `daily_limit.exceeded` rate over time
- `transactions.in_review` rate over time

**Row 5 — Reliability**
- `transactions.concurrency_conflicts` rate
- `outbox.relay_errors` rate
- `servicebus.dead_lettered` cumulative count
- DB health check status (0/1 from `/health/ready`)

**Row 6 — Infrastructure**
- SQL Server CPU / IO (from Azure Monitor or SQL Insights)
- Worker pod memory / CPU (from AKS metrics)
- OutboxRelayWorker relay lag (time from `OutboxMessage.CreatedAt` to `PublishedAt` — requires adding a `PublishedAt` timestamp to the entity)

### Alerts

| Alert | Condition | Severity | Action |
|---|---|---|---|
| Dead-letter messages | `servicebus.dead_lettered > 0 for 1m` | Warning | Investigate dead-letter queue; check for deserialisation or tenant config issues |
| Dead-letter accumulating | Dead-letter queue depth > 500 | Critical | Page on-call; may indicate systematic processing failure |
| Processing lag | `transactions.processing_duration p99 > 5s for 5m` | Warning | Check worker pod scaling and SQL Server performance |
| Concurrency conflict storm | `transactions.concurrency_conflicts > 100/min for 5m` | Warning | Hot-row contention; consider batched summary updates |
| Outbox relay error | `outbox.relay_errors > 0 for 2m` | Warning | Check relay worker logs; may indicate ASB connectivity issue |
| Queue depth high | ASB queue depth > 50,000 | Warning | Scale out Processor worker pods |
| Queue depth critical | ASB queue depth > 500,000 | Critical | Potential processing outage; page on-call |
| DB unhealthy | `/health/ready` returns non-200 for 1m | Critical | Page DBA; check SQL Server container / failover |
| API error rate | `http.server.request.duration{status=~"5.."} rate > 1% for 2m` | Critical | Check API pod logs; may indicate DB connectivity failure |
| Rejection spike | `transactions.rejected` rate > 5× 1h baseline for 10m | Warning | Possible bad data feed or upstream configuration change |
| Abandoned storm | `servicebus.abandoned > 50/min for 5m` | Warning | Repeated concurrency failures; check `MerchantDailySummary` hot rows |

### OpenTelemetry Pipeline

`ServiceDefaultsExtensions.ConfigureOpenTelemetry` configures:
- OTLP trace and metric export (`OTEL_EXPORTER_OTLP_ENDPOINT` environment variable)
- ASP.NET Core instrumentation (HTTP server request duration, active requests)
- HTTP client instrumentation (outbound calls)
- Runtime metrics (GC, thread pool, memory)
- Custom meter: `.AddMeter("Littlefish.TransactionProcessor")` — all 14 instruments above

Feed OTLP data to Grafana (via Grafana Alloy or OpenTelemetry Collector → Prometheus remote-write) or to Azure Monitor Application Insights.

---

## Technical Debt Register

### Resolved Since Initial Implementation

| Item | Resolution |
|---|---|
| **DB-queue polling** | Replaced with Transactional Outbox + Azure Service Bus. No DB polling in the processing path. |
| **Worker crash recovery** | ASB message locking handles in-flight crash for the processor. Message is redelivered on lock expiry. Dead-letter queue catches permanently failing messages. |
| **Test coverage** | 92 unit tests covering handlers, business rules, repositories, and worker logic. All passing. |
| **Stuck-in-Processing transactions** | Processor checks `Status != Received` — already-processed messages are idempotently acknowledged. (Partial — see open item below.) |
| **Correlation ID in API response** | `CorrelationId` is now returned in `IngestBatchResponse`. |

### Open Items

| Item | Description | Priority |
|---|---|---|
| **Authentication** | `[Authorize]` attribute exists on all controllers but no auth middleware is wired. Any caller can ingest transactions for any tenant without credentials. | **Critical** |
| **Stuck-in-Processing watchdog** | A transaction whose DB commit succeeded but whose `CompleteMessageAsync` was never called (worker crash) may be left in `Status = Processing` if ASB exhausts its delivery count. A background watchdog that resets transactions stuck in `Processing` for > N minutes is needed. | **High** |
| **OutboxMessage cleanup** | Published `OutboxMessage` rows are never deleted or archived. At 1,157 tx/sec, the table accumulates ~100M rows/day. Index scans and storage costs grow without bound. | **High** |
| **Dead-letter re-drive** | No consumer reads the ASB dead-letter queue. Failed messages are observable but not recoverable without manual intervention. | **High** |
| **Bulk duplicate pre-check** | `ExistsByTransactionIdAsync` is called once per item in the ingestion loop — O(N) individual DB reads. For a 5,000-item batch this is 5,000 round-trips. A single `WHERE TransactionId IN (...)` bulk query would reduce this to 1. | **Medium** |
| **REVERSAL business rules** | `TransactionType.Reversal` is persisted and passed all four rules. No domain rules govern reversals — they are treated as pass-through. Product must define the rule set. | **Medium** |
| **Within-batch Refund ordering** | A REFUND in the same batch as its originating PURCHASE may fail `RefundRequiresOriginalPurchaseRule` until the PURCHASE is processed. ASB does not guarantee ordering. Re-delivery eventually resolves this, but it creates spurious rejections. | **Medium** |
| **`MerchantDailySummary` hot-row contention** | At high TPS for popular merchants, the `rowversion` retry loop becomes a bottleneck. Batched updates (accumulate deltas, apply once per window) or Redis counters are needed before 100M tx/day. | **Medium** |
| **Correlation ID on ASB message** | `CorrelationId` is logged in the worker but is not set as the `ServiceBusMessage.CorrelationId` property. ASB tooling and dead-letter queues cannot correlate messages to ingestion batches without it. | **Low** |
| **`OutboxMessage.PublishedAt` timestamp** | The entity has no `PublishedAt` field. This makes it impossible to track relay lag (time from `CreatedAt` to publication) as a metric without adding the field. | **Low** |
| **Currency ISO 4217 validation** | `Currency` is stored as a 3-char uppercase string. No validation against the ISO 4217 standard is performed. Invalid codes (e.g. `"XXX"`) are accepted silently. | **Low** |
| **API versioning deprecation policy** | Only `v1` exists. The API versioning infrastructure (`Asp.Versioning`) is in place, but there is no documented deprecation policy or sunset header behaviour for when `v2` is introduced. | **Low** |
| **Worker HTTP health endpoints** | Workers are generic hosts — their `database` health check is registered in DI but not accessible over HTTP. Container orchestrators relying on HTTP probes cannot observe worker readiness without adding an HTTP server to the workers. | **Low** |
