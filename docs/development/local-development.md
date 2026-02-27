# Local Development

## Tools

1. [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) — IDE
2. [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) — runtime and build toolchain
3. [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Rancher Desktop](https://rancherdesktop.io/) — container runtime (required for SQL Server and ASB Emulator via Aspire)
4. [Mermaid CLI](https://github.com/mermaid-js/mermaid-cli) — diagram PNG generation

```cmd
npm install -g @mermaid-js/mermaid-cli
```

## Building

### .NET Aspire (recommended)

The Aspire AppHost orchestrates all dependencies automatically: SQL Server container, Grate migrations, Azure Service Bus Emulator, and all three .NET services.

```cmd
dotnet run --project src/Littlefish.TransactionProcessor.AppHost
```

Once running, the Aspire dashboard is available at `http://localhost:15888`. All service logs, traces, and metrics are visible there.

### Individual services

Run each service separately if you prefer to manage dependencies yourself.

```cmd
dotnet run --project src/Transaction.Presentation.Api
dotnet run --project src/Transaction.Worker.OutboxRelay
dotnet run --project src/Transaction.Worker.Processor
```

### Database

Database migrations are run automatically by the Aspire AppHost via a Grate container. The migration scripts are in:

```
src/dbTransactionProcessor/
├── createDatabase/      # CREATE DATABASE script
├── beforeMigration/     # Pre-migration permissions
└── up/
    ├── 0001–0011_*.sql  # Schema migrations
    └── LOCAL/           # Local seed data (tenants, lookup values, business rules)
```

To apply migrations manually against a running SQL Server instance:

```cmd
docker run --rm \
  -v $(pwd)/src/dbTransactionProcessor:/db \
  ghcr.io/erikbra/grate \
  --connectionstring "Server=localhost;Database=dbTransactionProcessor;..." \
  --sqlfilesdirectory /db
```

### Azure Service Bus Emulator

The ASB Emulator is configured by `src/Littlefish.TransactionProcessor.AppHost/ServiceBus.Emulator.Config.json`. The `transactions-ingest` queue is created automatically. No manual setup is required when using Aspire.

## Diagrams

Mermaid `.mermaid` source files live alongside their generated `.png` outputs in `docs/`. PNGs are regenerated automatically on each Debug build of `Transaction.Presentation.Api` via a post-build PowerShell step.

To regenerate manually:

```powershell
powershell -ExecutionPolicy Bypass -File build/mermaid/generate-mermaid-png.ps1
```

Or use the Mermaid CLI directly:

```cmd
mmdc -i docs/solution/diagrams/flow/ingest-batch.mermaid -o docs/solution/diagrams/flow/ingest-batch.png
```

## Testing

```cmd
dotnet test test/Transaction.Tests.Unit
dotnet test test/Transaction.Tests.Integration
```

Unit tests use an in-memory EF Core database. Integration tests use `WebApplicationFactory<Program>` with an in-memory database replacing SQL Server. No real Service Bus connection is required for tests.

## Branching

### Branch off main

Feature and bug-fix branches for this assessment:

1. Create branch: `feature/<feature-name>` from `main`
2. Implement changes
3. Run all tests: `dotnet test`
4. Raise a pull request back to `main`

### Hotfix

1. Create branch: `hotfix/<hotfix-name>` from `main`
2. Apply fix and run tests
3. Raise a pull request back to `main`
