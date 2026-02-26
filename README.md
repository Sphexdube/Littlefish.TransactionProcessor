# Littlefish Transaction Processor

A .NET 9 multi-tenant transaction processing system built with Clean Architecture and .NET Aspire.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [.NET Aspire workload / SDK 9.1.0](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required by Aspire to run SQL Server)
- [dotnet-ef global tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for migrations

Install the EF tool if you haven't already:
```bash
dotnet tool install --global dotnet-ef
```

## Running the Application

### 1. Clone and restore

```bash
git clone <repo-url>
cd Littlefish.TransactionProcessor
dotnet restore src/Littlefish.TransactionProcessor.sln
```

### 2. Start via Aspire AppHost (recommended)

The AppHost orchestrates SQL Server (in Docker), the REST API, and the background worker:

```bash
dotnet run --project src/Littlefish.TransactionProcessor.AppHost
```

The Aspire dashboard opens at **https://localhost:15888** and shows live logs and traces for all services.

| Service | URL (default) |
|---|---|
| Aspire Dashboard | https://localhost:15888 |
| Transaction API | https://localhost:7xxx (shown in dashboard) |
| Swagger UI | https://\<api-url\>/swagger |

### 3. Apply database migrations

Once SQL Server is running (Aspire starts it automatically), run:

```bash
dotnet ef database update \
  --project src/Transaction.Infrastructure.Persistence \
  --startup-project src/Transaction.Presentation.Api
```

### 4. Run tests

```bash
dotnet test src/Littlefish.TransactionProcessor.sln
```

## Architecture

```
Littlefish.TransactionProcessor/
├── src/
│   ├── Littlefish.TransactionProcessor.AppHost/        # .NET Aspire orchestration
│   ├── Littlefish.TransactionProcessor.ServiceDefaults/ # Shared telemetry / health checks
│   │
│   ├── Transaction.Presentation.Api/                   # ASP.NET Core REST API
│   ├── Transaction.Worker.Processor/                   # Background transaction processor
│   │
│   ├── Transaction.Application.Handlers/               # CQRS command/query handlers
│   ├── Transaction.Application.Models/                 # Request/response DTOs
│   ├── Transaction.Application.Validators/             # FluentValidation validators
│   ├── Transaction.Application.Constants/              # Shared constants / error messages
│   │
│   ├── Transaction.Domain.Entities/                    # Domain entities + exceptions
│   ├── Transaction.Domain.Interfaces/                  # Repository + unit-of-work contracts
│   ├── Transaction.Domain.Rules/                       # Business rule engine + 4 rules
│   ├── Transaction.Domain.Observability/               # IObservabilityManager abstraction
│   │
│   └── Transaction.Infrastructure.Persistence/         # EF Core, repositories, migrations
└── test/
    ├── Transaction.Tests.Unit/                          # Unit tests
    └── Transaction.Tests.Integration/                  # Integration tests
```

### Key Design Decisions

**Clean Architecture** — Domain is dependency-free. Application depends on Domain. Infrastructure and Presentation depend on Application.

**CQRS-style handlers** — Every API operation is handled by an `IRequestHandler<TRequest, TResponse>`. Controllers are thin and delegate via `ProcessRequest` / `ProcessRequestAccepted` helpers on `BaseController`.

**Multi-tenancy** — Every endpoint is scoped under `api/v{version}/tenants/{tenantId:guid}/...`. Tenant existence is validated in handlers (throws `NotFoundException` → 404 via middleware).

**Decoupled ingestion and processing** — The API ingests batches and records transactions with `Status = Received`. The Worker polls every 5 seconds and processes them asynchronously.

**Optimistic concurrency** — `MerchantDailySummary` has a SQL Server `rowversion` column. Concurrent workers racing to update the same merchant's daily total will retry up to 3 times on `DbUpdateConcurrencyException`.

## API Reference

### Ingest Transaction Batch

```
POST /api/v1/tenants/{tenantId}/transactions/:ingest
X-Correlation-Id: <uuid>
Content-Type: application/json

{
  "batchSize": 2,
  "transactions": [
    {
      "transactionId": "TXN-001",
      "merchantId":    "MERCHANT-001",
      "amount":        99.99,
      "currency":      "ZAR",
      "type":          "Purchase",
      "occurredAt":    "2025-01-15T10:00:00Z"
    },
    {
      "transactionId":          "TXN-002",
      "merchantId":             "MERCHANT-001",
      "amount":                 25.00,
      "currency":               "ZAR",
      "type":                   "Refund",
      "occurredAt":             "2025-01-15T11:00:00Z",
      "originalTransactionId":  "TXN-001"
    }
  ]
}
```

Returns `202 Accepted` with `{ "batchId": "<uuid>", "transactionCount": 2 }`.

### Get Transaction Status

```
GET /api/v1/tenants/{tenantId}/transactions/{transactionId}
```

Returns `200 OK` with transaction details, or `404 Not Found`.

### Get Merchant Daily Summary

```
GET /api/v1/tenants/{tenantId}/merchants/{merchantId}/daily-summary?date=2025-01-15
```

Returns `200 OK` with `{ "merchantId", "date", "totalAmount", "transactionCount" }`, or `404 Not Found`.

## Business Rules

Rules are evaluated in order. Processing stops on the first **Reject** result; a **Review** result allows continued processing.

| # | Rule | Outcome |
|---|---|---|
| 1 | `NegativePurchaseAmountRule` — amount ≤ 0 on a Purchase | Rejected |
| 2 | `RefundRequiresOriginalPurchaseRule` — Refund must reference an existing Purchase | Rejected |
| 3 | `DailyMerchantLimitRule` — projected Purchase total > tenant's daily limit | Rejected |
| 4 | `HighValueReviewRule` — amount > tenant's high-value threshold | Review (manual review required) |

## Transaction Lifecycle

```
Received → Processing → Processed
                      → Rejected  (failed a rule)
                      → Review    (passed rules but flagged for manual review)
```

## Health Checks

Both the API and the Worker expose `/healthz` (configured via Aspire service defaults) with an EF Core database connectivity check.
