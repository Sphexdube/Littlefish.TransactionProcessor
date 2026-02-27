# Deployment

## Projects

The solution contains three deployable services:

| Project | Role |
|---------|------|
| `Transaction.Presentation.Api` | REST API — accepts batch ingestion and query requests |
| `Transaction.Worker.OutboxRelay` | Background worker — relays outbox messages to Azure Service Bus |
| `Transaction.Worker.Processor` | Background worker — consumes Service Bus messages and processes transactions |

All three services share the same `dbTransactionProcessor` SQL Server database. The OutboxRelay and Processor workers require access to the Azure Service Bus `transactions-ingest` queue.

## Configuration

### Required connection strings / settings

| Key | Used by | Description |
|-----|---------|-------------|
| `ConnectionStrings:TransactionDb` | API, OutboxRelay, Processor | SQL Server connection string |
| `ConnectionStrings:messaging` | OutboxRelay, Processor | Azure Service Bus connection string |
| `ServiceBus:QueueName` | Processor | Queue name (default: `transactions-ingest`) |
| `OutboxRelay:QueueName` | OutboxRelay | Queue name (default: `transactions-ingest`) |
| `OutboxRelay:BatchSize` | OutboxRelay | Messages per relay cycle (default: `100`) |
| `OutboxRelay:PollingIntervalSeconds` | OutboxRelay | Relay polling interval (default: `1`) |

### Environment settings

| Setting | Development | Production |
|---------|-------------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Localhost` | `Production` |
| SQL Server | Container (Aspire) | Azure SQL |
| Azure Service Bus | ASB Emulator (Aspire) | Azure Service Bus namespace |
| OTLP endpoint | `http://localhost:4318` | Application Insights / OTLP collector |

## Database Migrations

Database schema is managed by [Grate](https://github.com/erikbra/grate). Migration scripts live in `src/dbTransactionProcessor/up/`. In local development, the Aspire AppHost runs the migration container automatically on startup.

For non-local environments, run the Grate migration container against the target SQL Server before deploying the services.
