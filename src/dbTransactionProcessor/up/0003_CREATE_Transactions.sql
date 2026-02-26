-- Transactions table
-- Each row is a single transaction within a batch
--
-- TransactionType enum values:
--   1 = Purchase, 2 = Refund, 3 = Reversal
--
-- TransactionStatus enum values:
--   1 = Received, 2 = Processing, 3 = Processed, 4 = Rejected, 5 = Review

CREATE TABLE [dbo].[Transactions]
(
    [Id]                      UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TenantId]                UNIQUEIDENTIFIER  NOT NULL,
    [BatchId]                 UNIQUEIDENTIFIER  NOT NULL,
    [TransactionId]           NVARCHAR(100)     NOT NULL,
    [MerchantId]              NVARCHAR(100)     NOT NULL,
    [Amount]                  DECIMAL(18, 2)    NOT NULL,
    [Currency]                NVARCHAR(3)       NOT NULL,
    [Type]                    INT               NOT NULL,
    [Status]                  INT               NOT NULL DEFAULT 1,
    [OccurredAt]              DATETIMEOFFSET    NOT NULL,
    [ProcessedAt]             DATETIMEOFFSET    NULL,
    [OriginalTransactionId]   NVARCHAR(100)     NULL,
    [RejectionReason]         NVARCHAR(500)     NULL,
    [Metadata]                NVARCHAR(MAX)     NULL,
    [CreatedAt]               DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt]               DATETIMEOFFSET    NULL,

    CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Transactions_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Batches] FOREIGN KEY ([BatchId])  REFERENCES [dbo].[Batches]  ([Id]) ON DELETE NO ACTION,

    -- Prevents duplicate transactionId per tenant
    INDEX [IX_Transactions_TenantId_TransactionId]         UNIQUE NONCLUSTERED ([TenantId], [TransactionId]),
    -- Supports DailyMerchantLimitRule queries
    INDEX [IX_Transactions_TenantId_MerchantId_OccurredAt] NONCLUSTERED ([TenantId], [MerchantId], [OccurredAt]),
    -- Supports pending-transaction poll by worker
    INDEX [IX_Transactions_Status]                         NONCLUSTERED ([Status]),
    -- Supports batch status rollups
    INDEX [IX_Transactions_BatchId]                        NONCLUSTERED ([BatchId])
);
GO
