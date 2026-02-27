# Solution

## Introduction

The Littlefish Transaction Processor is a multi-tenant transaction ingestion and processing system. Source systems submit batches of financial transactions to the REST API. Each accepted transaction is atomically persisted to the database alongside an outbox message. A background relay worker publishes those messages to Azure Service Bus, where a separate processing worker consumes them, evaluates configurable business rules, and finalises each transaction with a status of **Processed**, **Rejected**, or **Review**.

### Ingesting a batch

![Ingest Batch Flow](/docs/solution/diagrams/flow/ingest-batch.png)

### Processing a transaction

![Process Transaction Flow](/docs/solution/diagrams/flow/process-transaction.png)

## Business Rules

Business rules are stored in the database (`RuleWorkflows` / `BusinessRules` tables) and evaluated at processing time by the `RulesEngineAdapter` using the [RulesEngine](https://github.com/microsoft/RulesEngine) NuGet package. Rules use lambda expressions against a `TransactionRuleInput` context object.

The default `TransactionRules` workflow contains four rules evaluated in order:

| # | Rule | Expression | Outcome |
|---|------|-----------|---------|
| 1 | NegativePurchaseAmount | `TransactionType != "Purchase" \|\| Amount > 0` | Rejection |
| 2 | RefundRequiresOriginalPurchase | `TransactionType != "Refund" \|\| OriginalPurchaseExists == true` | Rejection |
| 3 | DailyMerchantLimit | `TransactionType != "Purchase" \|\| ProjectedDailyTotal <= DailyMerchantLimit` | Rejection |
| 4 | HighValueReview | `Amount > HighValueThreshold` | Review (manual) |

`DailyMerchantLimit` and `HighValueThreshold` are configurable per tenant.

## Solution Design

### High-level Architecture

#### Technologies

1. C# / .NET 9
2. ASP.NET Core with API versioning
3. Entity Framework Core 9 with SQL Server
4. Azure Service Bus (emulated locally via the ASB Emulator)
5. .NET Aspire for local orchestration and observability
6. [Grate](https://github.com/erikbra/grate) for database migrations

#### Guiding Principles

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) — strict layer separation (Presentation → Application → Domain → Infrastructure)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
  - [Single Responsibility](https://en.wikipedia.org/wiki/Single-responsibility_principle) — each handler, worker and repository has one concern
  - [Open/Closed](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle) — new business rules are added via DB rows, not code changes
  - [Liskov Substitution](https://en.wikipedia.org/wiki/Liskov_substitution_principle) — all repositories are substitutable via `IRepository<TEntity, TId>`
  - [Interface Segregation](https://en.wikipedia.org/wiki/Interface_segregation_principle) — `IRequestHandler<TRequest, TResponse>`, `IRuleEngine`, `IObservabilityManager`
  - [Dependency Inversion](https://en.wikipedia.org/wiki/Dependency_inversion_principle) — all cross-layer dependencies point inward via interfaces
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html) — guarantees at-least-once delivery to Service Bus without distributed transactions
- [Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html) — atomic DB operations across multiple repositories
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) — data access abstracted behind interfaces
- [CQRS (lightweight)](https://martinfowler.com/bliki/CQRS.html) — commands (`IngestBatchCommand`) and queries (`GetTransactionQuery`) share the same `IRequestHandler<TRequest, TResponse>` interface

#### Diagrams

### Solution Architecture

![Solution Architecture](/docs/solution/diagrams/flow/solution-architecture.png)

### Outbox Approach

The Outbox Pattern ensures that a transaction and its Service Bus message are always written together in a single database transaction. The `OutboxRelayWorker` polls for unpublished `OutboxMessage` rows every second and publishes them to the `transactions-ingest` queue.

![Outbox Approach](/docs/solution/diagrams/flow/outbox-approach.png)

### Entity Relationship Diagram

![dbTransactionProcessor ERD](/docs/solution/diagrams/erd/dbTransactionProcessor-entity-relationship-diagram.png)
