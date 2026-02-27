# Requirements Review

This document reviews the assessment requirements against the actual implementation, capturing gaps, assumptions, open questions, and the reasoning behind architectural decisions.

---

## What Is Unclear or Missing from the Requirements

### 1. REVERSAL type — no business rules specified

The spec lists `type (PURCHASE | REFUND | REVERSAL)`. Rules 1–4 cover PURCHASE and REFUND explicitly, but no rule governs REVERSAL. Questions that needed answering before shipping:
- Does a REVERSAL require an `originalTransactionId` (like a REFUND)?
- Does a REVERSAL affect the merchant's daily total (should it reduce it)?
- Can a REVERSAL reverse a REFUND, or only a PURCHASE?

**Decision taken:** `Reversal` is present in the `TransactionType` enum and is parsed and persisted correctly. No rule specifically targets it. A REVERSAL will currently pass all four rules and be marked `Processed`, which may or may not be correct depending on product intent. This is flagged as a known gap.

### 2. Batch size constraint is ambiguous

The spec says "Accepts a batch (100–5,000 items)." It is unclear whether a batch of fewer than 100 items should be rejected, or whether 100 is a target/recommended minimum.

**Decision taken:** FluentValidation rejects batches with 0 items. The upper bound of 5,000 is enforced. The lower bound of 100 is **not** enforced — partially-filled batches are accepted. This is more permissive than the spec implies, but rejecting a 50-item batch would be surprising to a caller.

### 3. Daily limit scope is underspecified

"Daily merchant PURCHASE limit per tenant (configurable)" — it is not clear whether "daily" means UTC calendar day, wall-clock day in tenant's timezone, or a rolling 24-hour window.

**Decision taken:** UTC calendar day derived from `transaction.OccurredAt.UtcDateTime`, consistent with how `MerchantDailySummary.Date` is set.

### 4. REFUND rule — timing of original transaction

The spec says "REFUND must reference an existing PURCHASE." It does not specify whether the original transaction must be `Processed`, or whether `Received` is sufficient.

**Decision taken:** The `RefundRequiresOriginalPurchaseRule` checks that a transaction with the given `OriginalTransactionId` exists as a `Processed` PURCHASE in the same tenant. A REFUND submitted in the same batch as its originating PURCHASE will fail this rule until the PURCHASE is processed — a known race condition documented in the limitations section.

### 5. Exactly-once ingestion response on duplicate

The spec does not specify what the API should return when a `transactionId` is re-submitted. Options are: return the original response (true idempotency), return an error, or silently drop it.

**Decision taken:** Duplicates within the same call are detected by `ExistsByTransactionIdAsync` and returned as a per-transaction rejection error in the batch response (`errors[]`) with a `DuplicateTransactionId` message. The batch is still accepted; other non-duplicate transactions in the same batch are processed normally.

### 6. Correlation ID propagation scope is undefined

The spec lists "Correlation ID propagation" as an observability requirement but does not specify whether it should be threaded through to Service Bus messages, stored on individual transaction records, or returned in every API response.

**Decision taken:** `X-Correlation-Id` is stored on the `Batch` record, propagated through the `OutboxMessage` payload into the worker's log scope, and returned in the `IngestBatchResponse`. It is not set as a Service Bus `CorrelationId` message property.

### 7. Authentication mechanism is unspecified

The spec does not define an authentication scheme. Controllers carry `[Authorize]` but no auth middleware is wired.

---

## Assumptions Made

| Area | Assumption |
|---|---|
| Multi-tenancy | Tenant isolation is enforced at the data layer — every DB query is scoped by `TenantId`. No shared data crosses tenant boundaries. |
| Currency | Stored as a 3-character uppercase string. No ISO 4217 validation beyond that. |
| Daily summary ownership | `MerchantDailySummary` is updated only from the Processor worker, not from ingestion. This means the summary reflects only `Processed` transactions, not `Received` ones. |
| Reversal | Treated as a valid type that passes all rules and is marked `Processed`. Product must define rules before it can be properly handled. |
| Outbox polling interval | 1-second interval with a batch of 100 messages is acceptable for near-real-time delivery. |
| Service Bus queue name | Hardcoded as `"transactions-ingest"` in configuration; override via `ServiceBus:QueueName`. |

---

## Questions for Product / Security / Ops

### Product
1. What are the rules for `REVERSAL`? Does it require `originalTransactionId`? Does it offset the daily merchant total?
2. Should a batch of fewer than 100 items be rejected at the API layer?
3. Is a REFUND that arrives in the same batch as its originating PURCHASE expected to succeed (requires within-batch dependency resolution)?
4. Should the daily limit be per UTC day, or per tenant timezone?
5. Should the API return `HTTP 202` even for fully-rejected batches (all items duplicate), or `HTTP 400`?
6. What is the SLA for processing latency — how quickly should a `Received` transaction reach `Processed`?

### Security
1. What authentication scheme should be used? (API key per tenant, OAuth2 client credentials, mTLS?)
2. Are tenant IDs discoverable, or should endpoints return `403` rather than `404` for tenants the caller is not authorised to access?
3. Are there data residency requirements? (SQL Server location, Service Bus namespace region)
4. What PII classification applies to `metadata` fields — do they need encryption at rest?

### Ops
1. What is the acceptable dead-letter queue depth before alerting?
2. Is there a requirement for dead-letter re-processing (re-drive consumer)?
3. How should published `OutboxMessage` records be cleaned up — archived or deleted after N days?
4. Are there regulatory retention requirements for `TransactionRecord` and rule evaluation outcomes?
5. What is the RTO/RPO expectation for the SQL Server instance?

---

## Functional Requirements

### 1. Ingestion API ✅

`POST /api/v1/tenants/{tenantId}/transactions/:ingest` with versioning (`X-Api-Version: 1.0`).

- Accepts a batch of 1–5,000 transactions (upper bound enforced; lower bound permissive).
- Validates tenant existence before writing any data — missing tenant returns `404 Not Found`.
- Persists `TransactionRecord` (Status = `Received`) and `OutboxMessage` atomically in a single `SaveChangesAsync` call (Outbox Pattern).
- Duplicate `transactionId` within a tenant is detected by `ExistsByTransactionIdAsync` before insert; the item is returned in `errors[]` and excluded from the batch count.
- Returns `202 Accepted` with `{ batchId, acceptedCount, rejectedCount, queuedCount, correlationId, errors }`.

### 2. Asynchronous Processing ✅

Three services handle the full lifecycle:

**`IngestBatchHandler` (API, synchronous):**
1. Validates tenant + duplicates.
2. Persists `TransactionRecord` (Status = `Received`) + `OutboxMessage` in one atomic write.
3. Returns `202 Accepted` immediately — no rule evaluation in the request path.

**`OutboxRelayWorker` (background, polling):**
1. Polls `OutboxMessages WHERE Published = false ORDER BY CreatedAt TAKE 100` every 1 second.
2. Publishes each payload to Azure Service Bus queue `"transactions-ingest"`.
3. Marks message `Published = true` and saves — only then is the message considered delivered.

**`TransactionProcessingWorker` (background, push-based):**
1. Receives messages from ASB via `ServiceBusProcessor` (`MaxConcurrentCalls = 4`, `AutoCompleteMessages = false`).
2. Deserialises `TransactionMessagePayload`. Dead-letters if null.
3. Loads `TransactionRecord` from DB. Dead-letters if not found.
4. Checks `Status == Received` — if already processed, `CompleteMessageAsync` (idempotent re-delivery).
5. Loads `Tenant`. Dead-letters if not found.
6. Begins DB transaction, sets `Status = Processing`, evaluates all four rules.
7. Sets final status (`Processed`, `Rejected`, or `Review`), updates `MerchantDailySummary` if Purchase.
8. Commits DB transaction, `CompleteMessageAsync`.
9. On `DbUpdateConcurrencyException`: rolls back, retries up to 3 times with fresh scope. Exhausted → `AbandonMessageAsync`.

### 3. Business Rules ✅

All four required rules are implemented in `Transaction.Domain.Rules` using the `RulesEngine` library (`RulesEngineAdapter` wraps `IRuleEngine`). Rules load from the embedded `TransactionRules.json` resource.

| Rule | Type | Outcome |
|---|---|---|
| `NegativePurchaseAmount` | Purchase, Amount ≤ 0 | Rejected |
| `RefundRequiresOriginalPurchase` | Refund, no Processed PURCHASE with `OriginalTransactionId` | Rejected |
| `DailyMerchantLimit` | Purchase, projected total > `Tenant.DailyMerchantLimit` | Rejected |
| `HighValueReview` | Any type, Amount > `Tenant.HighValueThreshold` | Review (non-blocking) |

Rules are evaluated in order. Processing stops on the first `Rejected` result. A `Review` result does not stop evaluation but marks the transaction accordingly.

**Gap — REVERSAL:** `TransactionType.Reversal` is parsed and persisted. No rules target it. It currently passes all four rules and is marked `Processed`. Product must define the REVERSAL rule set.

### 4. Transaction Query ✅

`GET /api/v1/tenants/{tenantId}/transactions/{transactionId}` — returns full transaction record including `Status`, `RejectionReason`, and timestamps. Returns `404` if not found in the given tenant.

### 5. Merchant Daily Summary ✅

`GET /api/v1/tenants/{tenantId}/merchants/{merchantId}/daily-summary?date=YYYY-MM-DD` — returns pre-aggregated values from `MerchantDailySummary`:

- `TotalAmount` — sum of all Processed PURCHASE amounts for the merchant on the date.
- `TransactionCount` — count of Processed PURCHASE transactions.
- `LastCalculatedAt` — UTC timestamp of the last `AddAmount` call.

The read is O(1) — a single indexed lookup on `(TenantId, MerchantId, Date)`, not a runtime aggregate scan.

---

## Non-Functional Requirements

### Idempotent Ingestion ✅

- `(TenantId, TransactionId)` unique index at the database level prevents duplicate inserts from racing requests.
- `ExistsByTransactionIdAsync` check in `IngestBatchHandler` provides an application-level idempotency guard that returns a friendly rejection error rather than letting the DB exception propagate.
- Worker checks `transaction.Status != Received` before processing — re-delivered ASB messages for already-processed transactions are acknowledged without re-processing.

### Concurrency Safety on Daily Limits ✅

- `MerchantDailySummary` carries a SQL Server `rowversion` (`IsRowVersion()` via EF Core).
- Two worker instances concurrently processing transactions for the same merchant on the same day will experience a `DbUpdateConcurrencyException` on the second writer.
- The worker retries up to 3 times with a fresh `AsyncServiceScope` — re-reading `TotalAmount` causes `DailyMerchantLimitRule` to re-evaluate against the committed total.
- On retry exhaustion: the message is `AbandonMessageAsync` — ASB redelivers it (up to the queue's `MaxDeliveryCount` before dead-lettering).

### Observability ✅

- Structured logging via `IObservabilityManager` / `ILogBuilder` throughout all handlers and workers.
- `X-Correlation-Id` captured at ingestion, stored on `Batch`, propagated through `OutboxMessage.Payload` into the worker log scope.
- OpenTelemetry configured in `ServiceDefaultsExtensions`: ASP.NET Core instrumentation, HTTP client instrumentation, runtime metrics, OTLP exporter.
- **14 custom metrics** via `IMetricRecorder` / `MetricRecorder` (`System.Diagnostics.Metrics.Meter("Littlefish.TransactionProcessor")`), registered in the OTLP pipeline via `.AddMeter("Littlefish.TransactionProcessor")`:

| Instrument | Type | Description |
|---|---|---|
| `transactions.ingested` | Counter | Accepted into a batch |
| `transactions.processed` | Counter | Successfully processed by worker |
| `transactions.rejected` | Counter | Rejected by rule or validation |
| `transactions.in_review` | Counter | Flagged for manual review |
| `batches.received` | Counter | Batches accepted by the API |
| `batches.completed` | Counter | Batches fully processed |
| `daily_limit.checks` | Counter | Daily limit rule evaluations |
| `daily_limit.exceeded` | Counter | Daily limit rejections |
| `outbox.messages_relayed` | Counter | Messages published to ASB |
| `outbox.relay_errors` | Counter | Outbox relay unexpected errors |
| `servicebus.dead_lettered` | Counter | Messages sent to dead-letter queue |
| `servicebus.abandoned` | Counter | Messages abandoned for redelivery |
| `transactions.concurrency_conflicts` | Counter | `DbUpdateConcurrencyException` events |
| `transactions.processing_duration` | Histogram (ms) | End-to-end processing time per message |

### Health Checks ✅

Three endpoints, separated by concern:

| Endpoint | Checks | Tag |
|---|---|---|
| `/alive` | `self` (always healthy) | `live` |
| `/health/ready` | `database` (DbContext ping) | `ready` |
| `/health` | All checks | — |

Aspire AppHost polls `transaction-api` via `.WithHttpHealthCheck("/health")`. Worker projects register the same `database` health check in DI but are generic hosts — they do not expose HTTP endpoints.

### Multi-Tenancy ✅

Every repository method accepts `TenantId` as a parameter and includes it in all queries. No cross-tenant query is possible through the repository interface. Tenant existence is validated in `IngestBatchHandler` before any tenant-scoped writes.

### Performance — No N+1 Queries ✅

- **Refund check:** `GetByTransactionIdAsync(tenantId, originalTransactionId)` — single indexed lookup. No scan.
- **Daily limit:** `GetByMerchantAndDateAsync` — single indexed lookup on pre-aggregated `MerchantDailySummary`. No `SUM` aggregate at query time.
- **Batch ingestion:** Each transaction in the batch issues an `ExistsByTransactionIdAsync` check (one query per item). For very large batches this is an O(N) read fan-out. An improvement would be a bulk `WHERE TransactionId IN (...)` pre-check.

---

## Architecture Decisions and Changes Made

### Transactional Outbox Pattern (added)

**Reason:** The spec permits and encourages outbox/inbox patterns. Tight coupling between the API and a message broker would mean a failed Service Bus publish could silently drop a transaction. The outbox guarantees delivery: the `TransactionRecord` and `OutboxMessage` are written atomically; if the API crashes before the relay publishes, the message is picked up on the next relay cycle.

**Implementation:** `OutboxRelayWorker` (new service) polls `OutboxMessages WHERE Published = false` every 1 second, publishes to ASB, marks as published. The Processor worker subscribes to ASB and processes messages.

### Azure Service Bus (added)

**Reason:** The spec explicitly lists "Message brokers" as an encouraged extension. The DB-queue polling approach described as a starting point has fundamental throughput and latency limitations at scale. ASB gives durable at-least-once delivery, message locking (prevents double-processing without a DB "claim" step), dead-letter queue, session support, and native back-pressure.

**Local dev:** `Aspire.Hosting.Azure.ServiceBus` runs the Microsoft Azure Service Bus Emulator in Docker — no Azure subscription needed.

### `RulesEngine` Library (added)

**Reason:** The spec says "Rules must not be hardcoded in the controller. Introduce a rule abstraction layer." Using `RulesEngine` (open-source, Microsoft-sponsored) provides a JSON-configurable rule store (`TransactionRules.json` embedded resource) without writing a custom rule evaluation framework. `RulesEngineAdapter` wraps it behind `IRuleEngine` so the library is not directly referenced by the application or domain layers.

### `MerchantDailySummary` as a Materialised Counter

**Reason:** Computing the daily total at read time would require `SUM(Amount) WHERE TenantId = X AND MerchantId = Y AND Date = D` — a potentially expensive scan. The pre-aggregated counter makes the daily summary read O(1) and makes the DailyMerchantLimit rule check O(1). The trade-off is write-path complexity and the concurrency retry loop.

### FluentValidation at the API Layer

Structural validation (required fields, type enum values, currency format, amount bounds) runs in `IngestTransactionBatchRequestValidator` before any DB interaction. Business-rule validation (does the original transaction exist?) runs in the domain rules inside the worker. This keeps the API fast (reject malformed batches without touching the DB) and keeps business logic in the domain.

### Grate for Schema Migrations (added)

SQL schema is managed by Grate migration scripts under `src/dbTransactionProcessor/up/`. The AppHost runs Grate as a Docker container (`erikbra/grate`) after SQL Server starts and before the .NET services start. This eliminates `dotnet ef` tooling from the deployment path and aligns with how the reference project manages schema.

---

## Known Limitations and Future Work

| Area | Current State | Recommended Next Step |
|---|---|---|
| **REVERSAL rules** | Type is persisted; no business rules target it | Product must define REVERSAL behaviour; add rules to `TransactionRules.json` |
| **Within-batch Refund ordering** | A REFUND in the same batch as its PURCHASE fails rule 2 until the PURCHASE is processed (ASB ordering is not guaranteed) | Process within-batch dependencies in topological order, or accept eventual consistency with re-delivery |
| **OutboxMessage cleanup** | Published `OutboxMessage` rows are never deleted or archived — the table grows unbounded | Add a background cleanup job or soft-delete policy after N days |
| **Dead-letter re-drive** | Dead-lettered messages are observable in the ASB emulator but there is no re-drive consumer | Implement a dead-letter monitor / re-drive worker |
| **Authentication** | `[Authorize]` attribute exists; no auth middleware wired — any caller can ingest for any tenant | Integrate ASP.NET Core authentication with an appropriate scheme (OAuth2 client credentials or API key per tenant) |
| **Bulk duplicate pre-check** | `ExistsByTransactionIdAsync` is called once per transaction item in the batch (O(N) reads) | Replace with a bulk `WHERE TransactionId IN (...)` query to reduce round-trips for large batches |
| **Currency validation** | Stored as a 3-char uppercase string; no ISO 4217 validation | Add a lookup table or regex validator |
| **Correlation ID on ASB** | `CorrelationId` in `TransactionMessagePayload` is logged but not set as the ASB message `CorrelationId` property | Set `ServiceBusMessage.CorrelationId` on publish for end-to-end trace correlation in ASB tooling |
| **API versioning deprecation policy** | Only `v1` exists; versioning infrastructure is in place | Define and document a deprecation policy before adding `v2` |
| **Worker HTTP health checks** | Workers are generic hosts with no HTTP server — health checks are registered in DI but not accessible over HTTP | Either add ASP.NET Core to workers (adds HTTP overhead) or rely on process-level liveness probes in the container orchestrator |
