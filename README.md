# Littlefish Transaction Processor

A .NET 9 multi-tenant transaction processing system built with Clean Architecture and .NET Aspire. The service accepts batches of financial transactions via a REST API, persists them atomically alongside an outbox record, relays those records to Azure Service Bus, and evaluates them against configurable business rules in an independent worker process.

## Table of Contents

1. [Solution](/docs/solution/solution.md)
2. [Application](/docs/application/application.md)
3. [Deployment](/docs/deployment/deployment.md)
4. [Local Development](/docs/development/local-development.md)

## Database Architecture

### Database (`dbTransactionProcessor`)

| Table | Description |
|---|---|
| `Transactions` | Core transaction records — one row per transaction |
| `Batches` | Ingestion batch metadata (accepted/rejected counts) |
| `Tenants` | Multi-tenant configuration including daily limit and high-value threshold |
| `MerchantDailySummaries` | Aggregated daily purchase spend per merchant per tenant |
| `OutboxMessages` | Transactional outbox — unpublished messages queued for Service Bus relay |

### Lookup Tables

| Table | Values |
|---|---|
| `TransactionTypes` | `Purchase`, `Refund` |
| `TransactionStatuses` | `Received`, `Processing`, `Processed`, `Rejected`, `Review` |
| `BatchStatuses` | `Pending`, `Processing`, `Completed`, `PartiallyRejected` |

## Key Features

- Multi-tenant transaction ingestion via a batch REST API (`POST /transactions/:ingest`)
- Transactional Outbox Pattern — DB write and outbox record are committed atomically; no lost messages on crash
- Azure Service Bus relay via `OutboxRelayWorker` (polls outbox at 1-second intervals, batch size 100)
- Business rule evaluation in `TransactionProcessingWorker` — four rules evaluated per transaction:
  - Negative purchase amount → Rejected
  - Refund without a matching original Purchase → Rejected
  - Daily merchant purchase limit exceeded → Rejected
  - Amount exceeds high-value threshold → Flagged for Review
- Optimistic concurrency with up to 3 retries on `MerchantDailySummary` (`rowversion` column)
- Dead-letter, abandon, and retry patterns on Service Bus message processing
- 14 custom OpenTelemetry metrics exported via OTLP (`transactions.*`, `batches.*`, `outbox.*`, `servicebus.*`)
- Three health endpoints: `/health` (all checks), `/health/ready` (readiness), `/alive` (liveness)
- Aspire-orchestrated local development — SQL Server + Azure Service Bus emulator run in Docker; Grate applies schema migrations automatically on startup

**<p style="text-align: center;">Copyright © 2025 Littlefish</p>**
