-- MerchantDailySummaries table
-- Pre-aggregated daily purchase totals per merchant per tenant
-- Used by DailyMerchantLimitRule and the daily-summary GET endpoint
--
-- Version is a SQL Server rowversion (auto-updated on every write) used for
-- optimistic concurrency control in the background worker

CREATE TABLE [dbo].[MerchantDailySummaries]
(
    [Id]                 UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TenantId]           UNIQUEIDENTIFIER  NOT NULL,
    [MerchantId]         NVARCHAR(100)     NOT NULL,
    [Date]               DATE              NOT NULL,
    [TotalAmount]        DECIMAL(18, 2)    NOT NULL DEFAULT 0.00,
    [TransactionCount]   INT               NOT NULL DEFAULT 0,
    [LastCalculatedAt]   DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Version]            ROWVERSION        NOT NULL,
    [CreatedAt]          DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt]          DATETIMEOFFSET    NULL,

    CONSTRAINT [PK_MerchantDailySummaries] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_MerchantDailySummaries_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE NO ACTION,

    -- Enforces one summary row per tenant/merchant/day
    INDEX [IX_MerchantDailySummaries_TenantId_MerchantId_Date] UNIQUE NONCLUSTERED ([TenantId], [MerchantId], [Date])
);
GO
