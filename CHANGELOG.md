# Change Log
All notable changes to this project will be documented in this file.

## 1.0.0 - 2026-02-26

### Added
- Scaffolded the solution
- Clean Architecture with Domain, Application, Infrastructure, and Presentation layers
- Transaction ingestion API endpoint (POST /tenants/{tenantId}/transactions/batches)
- Transaction retrieval endpoint (GET /tenants/{tenantId}/transactions/{transactionId})
- Merchant daily summary endpoint (GET /tenants/{tenantId}/merchants/{merchantId}/daily-summaries/{date})
- Business rules: negative purchase amount, refund requires original purchase, daily merchant limit, high-value review
- Background worker for async transaction processing
- Grate-based SQL Server schema migrations
- .NET Aspire AppHost orchestration for local development

### Changed
N/A new solution

### Fixed
N/A new solution
