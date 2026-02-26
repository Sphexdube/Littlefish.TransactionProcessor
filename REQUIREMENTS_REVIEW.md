# Requirements Review

This document reviews the assessment requirements, captures design decisions, and notes known trade-offs.

---

## Functional Requirements

### Multi-Tenant Batch Ingestion ✅

The `POST /api/v1/tenants/{tenantId}/transactions/:ingest` endpoint accepts a batch of 100–5000 transactions.

- Batch size declared in the payload and validated against the actual items count.
- Each transaction is persisted with `Status = Received` and linked to a `Batch` record that carries the `X-Correlation-Id` header.
- Tenant existence is validated in `IngestBatchHandler` before any data is written; a missing tenant returns `404 Not Found`.
- Duplicate `transactionId` within a tenant is enforced by a unique database index, preventing double-ingestion at the persistence layer.

### Asynchronous Processing ✅

`TransactionProcessingWorker` is a `BackgroundService` that:

1. Polls every 5 seconds for transactions with `Status = Received` (batch of 50).
2. Claims each one by setting `Status = Processing` inside a DB transaction before any rule evaluation, preventing two worker instances from picking up the same record.
3. Evaluates all four business rules via `IRuleEngine`.
4. Sets the final status to `Processed`, `Rejected`, or `Review`.

### Business Rules ✅

All four rules are implemented in `Transaction.Domain.Rules`:

| Rule | Logic |
|---|---|
| `NegativePurchaseAmountRule` (order 1) | Rejects Purchase transactions where `Amount <= 0` |
| `RefundRequiresOriginalPurchaseRule` (order 2) | Rejects Refund transactions that reference a `transactionId` that does not exist as a processed Purchase in the same tenant |
| `DailyMerchantLimitRule` (order 3) | Rejects Purchases that would take the merchant's running daily total above `Tenant.DailyMerchantLimit` |
| `HighValueReviewRule` (order 4) | Flags transactions above `Tenant.HighValueThreshold` as `Review` (processing continues) |

The rule engine runs rules in ascending `Order`, stops on the first `Rejected` result, but continues through a `NeedsReview` result.

### Transaction Status Query ✅

`GET /api/v1/tenants/{tenantId}/transactions/{transactionId}` returns the full transaction record including status, rejection reason, and processed timestamp. Returns `404` if not found.

### Merchant Daily Summary ✅

`GET /api/v1/tenants/{tenantId}/merchants/{merchantId}/daily-summary?date=YYYY-MM-DD` returns the pre-computed daily totals from `MerchantDailySummary`. This table is maintained by the worker as each Purchase is processed, so the read is O(1) index lookup rather than a full aggregate scan.

---

## Non-Functional Requirements

### Idempotent Ingestion ✅

A unique index on `(TenantId, TransactionId)` in the `Transactions` table ensures that re-submitting the same `transactionId` for the same tenant does not create duplicate records. The worker's "claim via DB transaction" pattern prevents double-processing.

### Concurrency Safety on Daily Limits ✅

`MerchantDailySummary` carries a SQL Server `rowversion` (`IsRowVersion()`) column. When two worker instances concurrently process transactions for the same merchant on the same day, the second writer will receive a `DbUpdateConcurrencyException`. The worker retries up to 3 times with a fresh EF scope (re-reading the latest `TotalAmount`), which causes `DailyMerchantLimitRule` to re-evaluate against the committed total. On retry exhaustion the transaction is reset to `Received` and will be re-picked on the next poll cycle.

### Observability ✅

- Structured logging with `ILogger` throughout (correlation ID, tenant ID, transaction ID in log scope).
- `IObservabilityManager` / `ILogBuilder` abstraction mirrors the reference project pattern.
- OpenTelemetry configured via `Littlefish.TransactionProcessor.ServiceDefaults` (OTLP exporter, ASP.NET Core and HTTP instrumentation, runtime metrics).
- `X-Correlation-Id` is captured at ingestion and stored on the `Batch` record; the worker propagates it into the log scope for every transaction it processes.

### Health Checks ✅

Both the API and Worker register `AddDbContextCheck<TransactionDbContext>("database")`. The `/healthz` endpoint is exposed via Aspire service defaults.

### .NET Aspire Orchestration ✅

`Littlefish.TransactionProcessor.AppHost` uses `Aspire.Hosting.SqlServer` to provision a SQL Server Docker container in development, passes the connection string to both services via `WithReference`, and blocks startup with `WaitFor` until the database is ready.

---

## Design Decisions and Trade-offs

### DB-queue vs Message Bus

Requirements do not mandate a message broker, and the spec calls for a self-contained, runnable solution. A simple "inbox" pattern using a `Status = Received` column on the `Transactions` table gives durable, crash-safe queuing without operational dependencies. The trade-off is limited horizontal throughput — adding a dedicated queue (Azure Service Bus, RabbitMQ) would be the next step for high-volume production use.

### `MerchantDailySummary` as a Materialised Counter

Computing the daily total at query time with `SUM(Amount)` would be correct but would scan all transactions for a merchant per day. Maintaining a pre-aggregated counter is faster at read time. The trade-off is complexity in the write path (concurrency retries) and the need to keep the counter consistent with the `Transactions` table (both updated in the same DB transaction).

### Single DB Transaction Per Transaction Record

The worker wraps the claim + rule evaluation + status update + summary upsert in a single DB transaction. This gives atomicity but means a long-running rule could hold a lock. For the current rule set (all in-memory evaluations) this is not a concern. If rules needed to call external services, a saga or outbox pattern would be more appropriate.

### Tenant Configuration in the Database

`Tenant.DailyMerchantLimit` and `Tenant.HighValueThreshold` are stored per-tenant in the database. This supports different limits per tenant without code changes. The trade-off is that rule evaluation requires a DB-loaded `Tenant` entity (always fetched as part of the transaction load via `Include`).

### FluentValidation at the API Layer

All structural validation (required fields, valid amounts, valid currency codes, correct refund shape) happens in `IngestTransactionBatchRequestValidator` before the handler runs. Business rule validation (does the original transaction exist?) happens in the domain rules inside the worker. This separation keeps the API fast (reject malformed batches immediately) and keeps business logic in the domain layer.

---

## Known Limitations and Future Work

| Area | Current State | Recommended Next Step |
|---|---|---|
| Queue | Database polling (5s interval) | Replace with Azure Service Bus or similar for lower latency and horizontal scaling |
| Worker instances | Multiple instances will compete via "claim" status, but no leader election | Use distributed lock or partitioned queue for clean horizontal scale-out |
| Migrations | `InitialCreate` migration exists; no seed data | Add seed script for test tenants |
| Authentication | `[Authorize]` attribute present on controllers; no JWT/API-key middleware wired | Integrate ASP.NET Core authentication middleware with a suitable scheme |
| Refund validation timing | Rule 2 only passes if the original Purchase has `Status = Processed` — a Refund arriving in the same batch as its Purchase may fail until the Purchase is processed | Process within-batch dependencies in topological order or accept eventual consistency with retry |
| Test coverage | Stubs in place for unit and integration test projects | Implement tests for rules, handlers, and integration scenarios |
