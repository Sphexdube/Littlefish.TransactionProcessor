# Application

## API Checklist

- [x] Multi-tenant transaction batch ingestion
- [x] Idempotent transaction processing (duplicate detection)
- [x] Business rule evaluation (configurable per tenant)
- [x] Outbox pattern for reliable Service Bus delivery
- [x] Optimistic concurrency on MerchantDailySummary
- [x] API versioning (v1)
- [x] FluentValidation request validation
- [x] Structured observability (IObservabilityManager)
- [x] Health checks
- [x] Swagger / OpenAPI

## API Endpoints

| Endpoint | Verb | Description |
|----------|------|-------------|
| `/api/v1/tenants/{tenantId}/transactions:ingest` | POST | Ingest a batch of transactions for a tenant. Returns a 202 Accepted with batch summary. |
| `/api/v1/tenants/{tenantId}/transactions/{transactionId}` | GET | Retrieve a single transaction record by its external transaction ID. |
| `/api/v1/tenants/{tenantId}/merchants/{merchantId}/daily-summary` | GET | Retrieve the daily purchase summary for a merchant on a given date (`?date=yyyy-MM-dd`). |

## Additional Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Health check endpoint (EF Core DB connectivity) |
| `/swagger` | OpenAPI / Swagger UI (non-production environments) |

## Transaction Statuses

| Status | Value | Description |
|--------|-------|-------------|
| Received | 1 | Transaction accepted into the batch; awaiting processing |
| Processing | 2 | Message consumed from Service Bus; rule evaluation in progress |
| Processed | 3 | All rules passed; transaction accepted |
| Rejected | 4 | A business rule failed; see `RejectionReason` |
| Review | 5 | Transaction flagged for manual review (high-value threshold exceeded) |

## Batch Statuses

| Status | Value | Description |
|--------|-------|-------------|
| Received | 1 | Batch created and transactions queued |
| Processing | 2 | At least one transaction is being processed |
| Completed | 3 | All transactions processed successfully |
| PartiallyCompleted | 4 | Some transactions rejected |
| Failed | 5 | Batch processing failed |

## Transaction Types

| Type | Value | Description |
|------|-------|-------------|
| Purchase | 1 | A standard merchant purchase |
| Refund | 2 | A refund against an existing purchase (`OriginalTransactionId` required) |
| Reversal | 3 | A reversal of a prior transaction |

## Business Rules

Rules are stored in the `RuleWorkflows` / `BusinessRules` tables and evaluated by the `TransactionRules` workflow at processing time.

| Rule | Error Message | Applies To |
|------|--------------|-----------|
| NegativePurchaseAmount | PURCHASE amount cannot be negative | Purchase |
| RefundRequiresOriginalPurchase | REFUND must reference an existing PURCHASE transaction | Refund |
| DailyMerchantLimit | Daily merchant purchase limit would be exceeded | Purchase |
| HighValueReview | _(flags for review — no rejection)_ | All |

## Logs

Log output is structured via `IObservabilityManager` and routed through the standard .NET `ILogger` pipeline. When running under .NET Aspire, logs are visible in the Aspire dashboard at `http://localhost:15888` (OTLP endpoint `http://localhost:4318`).

Key log message constants are defined in `Transaction.Domain.Observability.LogMessages`.

## Message Processing

Dead-letter reasons used when the `TransactionProcessingWorker` cannot process a message:

| Reason | Description |
|--------|-------------|
| DeserializationFailed | Message body could not be deserialised to `TransactionMessagePayload` |
| NotFound | `TransactionRecord` does not exist in the database |
| TenantNotFound | Tenant referenced in the message does not exist |
| UnexpectedError | An unhandled exception occurred during processing |

Messages are abandoned (returned to the queue) on `DbUpdateConcurrencyException` with up to 3 retry attempts before abandonment for Service Bus redelivery.
