# System Ownership

This document addresses production ownership concerns: scaling bottlenecks, exactly-once processing strategy, observability dashboards/alerts, and known tech debt.

---

## Scaling to 100 Million Transactions Per Day

### Throughput Baseline

100 M transactions/day ≈ **1,157 transactions/second** (average), with realistic peaks of 3–5× that (~5,000 TPS).

The current architecture uses:
- A single SQL Server instance
- A polling-based DB queue (5s interval, batch 50)
- A single Worker process

At this volume that breaks immediately.

### What Breaks First, In Order

#### 1. The DB-queue polling pattern

The worker polls `Transactions WHERE Status = Received ORDER BY CreatedAt TAKE 50`. At high write rates:
- This scan turns into a hot range lock on the `Status` index.
- Multiple worker instances create lock contention on the same rows.
- 5-second polling means up to 5s of latency before a transaction is picked up.

**Fix:** Replace DB polling with a durable message queue (Azure Service Bus, RabbitMQ). The API publishes a message on ingestion; workers consume from the queue. This decouples ingestion throughput from processing throughput, eliminates polling, and gives back-pressure naturally.

#### 2. The `MerchantDailySummary` hot row

For a popular merchant, every processed Purchase updates the same `MerchantDailySummary` row. At 5,000 TPS with 10 merchants per worker partition, a popular merchant row is updated hundreds of times per second — even with RowVersion retries this becomes a serialisation point.

**Fix:** Accumulate updates in memory per worker over a short window (e.g. 1 second), then apply one batched `UPDATE ... SET TotalAmount += @delta` per merchant per window. This reduces the hot-row update rate by orders of magnitude. The batch update is still within a DB transaction so atomicity is preserved.

#### 3. The SQL Server write path

At 5,000 TPS writes on a single SQL Server instance, the transaction log becomes the bottleneck:
- `Transactions` table receives ~5,000 inserts/sec from the API.
- Each insert + update cycle from the worker is an additional ~5,000 writes/sec.
- Index maintenance on `Status`, `TenantId+MerchantId+OccurredAt` adds overhead.

**Fix:**
- Scale out reads with read replicas (or AlwaysOn AG readable secondary).
- Partition the `Transactions` table by `TenantId` or time range to reduce index hot-spots.
- Consider switching to an append-only ledger model for ingestion and projecting status via events.

#### 4. The API ingestion path

A single API instance accepting 1,157 batches/sec (at average batch size 1,000) is manageable for ASP.NET Core on modern hardware, but:
- Parsing and validating 1,000-item JSON arrays synchronously per request will cause thread starvation under load.
- `FluentValidation.AspNetCore` with synchronous validators on large arrays blocks threadpool threads.

**Fix:** Horizontal scale-out behind a load balancer (AKS pods). Ensure all I/O in the ingest path is async (it is). Consider streaming JSON parsing for very large batches.

#### 5. The daily merchant limit rule under concurrency

At scale, multiple worker instances will process thousands of transactions for the same merchant simultaneously. The RowVersion retry loop works for 2–3 concurrent conflicts, but at hundreds of concurrent updates it degrades to a retry storm.

**Fix:** Use pessimistic locking (SELECT ... WITH (UPDLOCK, ROWLOCK)) for the MerchantDailySummary update, or (better) use the batched-update approach described above. Alternatively, enforce the limit probabilistically with a Redis counter (INCR with EXPIRE) as a fast guard, and reconcile with the DB counter asynchronously.

### Recommended Scale-Out Architecture

```
                     ┌─────────────────────────────────────┐
                     │           API (n instances)          │
                     │  POST /transactions/:ingest          │
                     └──────────────────┬──────────────────┘
                                        │ publish batch messages
                                        ▼
                     ┌─────────────────────────────────────┐
                     │      Azure Service Bus / RabbitMQ   │
                     │      (partitioned by TenantId)      │
                     └──────────────────┬──────────────────┘
                                        │ consume
                     ┌──────────────────▼──────────────────┐
                     │       Workers (n instances)         │
                     │  one partition per worker            │
                     └──────────────────┬──────────────────┘
                                        │ write
                     ┌──────────────────▼──────────────────┐
                     │         SQL Server (primary)         │
                     │  + read replicas for status queries  │
                     └─────────────────────────────────────┘
```

---

## Exactly-Once Processing Strategy

### Problem

At-least-once delivery (retries on failure) combined with a stateful write (update `Status`) can produce duplicates if the worker crashes after writing but before acknowledging the message.

### Current Approach (At-Most-Once-Friendly with Retry)

The current system uses a DB-queue pattern:
- Transactions are claimed by setting `Status = Processing` inside a DB transaction before processing.
- If the worker crashes mid-processing, the transaction stays in `Processing` and is never re-picked (the poll filters `Status = Received`).

This is safe against double-processing but creates **stuck transactions** on crash.

### Recommended Exactly-Once Strategy

1. **Idempotent unique key at ingestion** — the `(TenantId, TransactionId)` unique index rejects duplicates at ingestion time. A retried `POST /ingest` with the same `transactionId` is a no-op (EF will throw `DbUpdateException` which can be caught and mapped to a conflict response or ignored if the intent is idempotent ingestion).

2. **Claim-check pattern** — when picking up a transaction, set `Status = Processing` and record `WorkerId` and `ClaimedAt`. A separate "watchdog" (or the next poll cycle) can detect transactions stuck in `Processing` for more than a configurable timeout (e.g. 2 minutes) and reset them to `Received`. This recovers from worker crashes without double-processing.

3. **Message bus outbox** — if migrating to a message queue, use the outbox pattern: the API inserts the transaction record and the outbox message in the same DB transaction. A relay publishes confirmed outbox messages to the queue. Workers acknowledge messages only after a successful DB write. Combined with the idempotent `(TenantId, TransactionId)` key, this achieves exactly-once end-to-end.

4. **Idempotent rule evaluation** — the rule engine is pure in-memory evaluation; re-running it on the same `RuleContext` with the same `MerchantDailySummary.TotalAmount` produces the same result, making re-processing safe.

---

## Dashboards and Alerts

### Key Metrics to Track

| Metric | Description | Alert Threshold |
|---|---|---|
| `transactions_ingested_total` | Counter: transactions accepted by the API | — |
| `transactions_processed_total{status}` | Counter by `Processed`, `Rejected`, `Review` | — |
| `transactions_processing_lag_seconds` | Histogram: time from `CreatedAt` to `ProcessedAt` | > 30s p99 |
| `transactions_pending_count` | Gauge: `COUNT(*) WHERE Status = Received` | > 10,000 |
| `transactions_stuck_processing_count` | Gauge: transactions in `Processing` > 2 minutes | > 0 |
| `rule_rejections_total{rule}` | Counter by rule name | spike > 5× baseline |
| `concurrency_retries_total` | Counter: RowVersion retry attempts | > 100/min |
| `batch_ingest_duration_seconds` | Histogram: API handler duration | p99 > 500ms |
| `db_health` | 0/1 from health check endpoint | 0 |

### Recommended Dashboard Layout (e.g. Grafana)

**Row 1 — Throughput**
- Ingestion rate (transactions/min), Processing rate (transactions/min), Rejection rate (%)

**Row 2 — Latency**
- Processing lag p50/p95/p99, API response time p50/p95/p99

**Row 3 — Queue Health**
- Pending queue depth (trend + current), Stuck-in-Processing count

**Row 4 — Business Rules**
- Rejection breakdown by rule (pie + time series), Review flagging rate

**Row 5 — Infrastructure**
- SQL Server CPU/IO, Worker pod CPU/memory, DB health check status

### Alerts

| Alert | Condition | Severity |
|---|---|---|
| Queue depth high | `transactions_pending_count > 10,000 for 5m` | Warning |
| Queue depth critical | `transactions_pending_count > 100,000 for 2m` | Critical |
| Processing lag SLA | `p99 lag > 60s for 5m` | Warning |
| Stuck transactions | `transactions_stuck_processing_count > 0 for 10m` | Warning |
| DB unhealthy | `db_health == 0 for 1m` | Critical |
| Rejection spike | `rate(rule_rejections_total[5m]) > 5 * rate(rule_rejections_total[1h])` | Warning |
| API error rate | `rate(http_server_request_duration_count{status=~"5.."}[5m]) / rate(http_server_request_duration_count[5m]) > 0.01` | Critical |

### OpenTelemetry Integration

The `Littlefish.TransactionProcessor.ServiceDefaults` project configures:
- OTLP trace and metric export (configure `OTEL_EXPORTER_OTLP_ENDPOINT` in environment)
- ASP.NET Core and HTTP client instrumentation
- Runtime metrics (GC, thread pool)

Feed OTLP data into Grafana (via Prometheus remote-write) or Azure Monitor Application Insights.

---

## Tech Debt Register

| Item | Description | Priority |
|---|---|---|
| **Authentication** | `[Authorize]` attribute exists but no auth middleware is wired. Any caller can ingest transactions for any tenant. | High |
| **Stuck-Processing recovery** | Transactions that crash mid-processing stay in `Processing` forever. A watchdog / timeout reset is needed for resilience. | High |
| **Test coverage** | Unit and integration test projects exist but contain no tests. Business rules, handlers, and the worker retry logic all need coverage. | High |
| **Seed data / migrations script** | No mechanism to create test tenants. Running the system requires manual SQL inserts. | Medium |
| **DB queue → message bus** | Polling every 5s is inefficient at scale. Moving to a proper queue reduces latency and improves horizontal scaling. | Medium |
| **Worker crash recovery** | See "Stuck-Processing" above. Also: the worker has no dead-letter queue; a transaction that consistently errors is retried on every poll cycle forever. | Medium |
| **Refund ordering** | A Refund in the same batch as its originating Purchase will fail rule 2 until the Purchase is processed (a race condition). Topological ordering within a batch or a compensating retry would fix this. | Medium |
| **Correlation ID end-to-end** | Correlation ID is stored on `Batch` and propagated into the worker's log scope. It is not forwarded as an HTTP header to downstream services (none currently exist) and is not returned in API responses. | Low |
| **API versioning** | Only `v1` exists. The versioning infrastructure is in place but there is no deprecation policy or migration guide. | Low |
| **Currency validation** | `Currency` is stored as a 3-char string with no ISO 4217 validation beyond length. | Low |
| **`IsAspireSharedProject` workload flag** | `ServiceDefaults` still carries `<IsAspireSharedProject>true</IsAspireSharedProject>` which was a workload-era marker. It builds fine on SDK 10 but should be cleaned up. | Low |
