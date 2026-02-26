-- Batches table
-- Represents a single ingest request carrying one or more transactions
--
-- BatchStatus enum values:
--   1 = Received, 2 = Processing, 3 = Completed, 4 = PartiallyCompleted, 5 = Failed

CREATE TABLE [dbo].[Batches]
(
    [Id]             UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TenantId]       UNIQUEIDENTIFIER  NOT NULL,
    [Status]         INT               NOT NULL DEFAULT 1,
    [TotalCount]     INT               NOT NULL DEFAULT 0,
    [AcceptedCount]  INT               NOT NULL DEFAULT 0,
    [RejectedCount]  INT               NOT NULL DEFAULT 0,
    [QueuedCount]    INT               NOT NULL DEFAULT 0,
    [CorrelationId]  NVARCHAR(100)     NOT NULL,
    [CompletedAt]    DATETIMEOFFSET    NULL,
    [CreatedAt]      DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt]      DATETIMEOFFSET    NULL,

    CONSTRAINT [PK_Batches] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Batches_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE NO ACTION,

    INDEX [IX_Batches_TenantId] NONCLUSTERED ([TenantId]),
    INDEX [IX_Batches_Status]   NONCLUSTERED ([Status])
);
GO
