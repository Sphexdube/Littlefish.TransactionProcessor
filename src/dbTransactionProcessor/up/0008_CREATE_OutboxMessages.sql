-- OutboxMessages table
-- Stores pending messages to be published to the message bus by the OutboxRelay worker.
-- Written atomically alongside Transactions in the same DB transaction during ingestion.
-- Once published to Azure Service Bus the Published flag is set to 1 and the row is retained
-- for audit purposes.

CREATE TABLE [dbo].[OutboxMessages]
(
    [Id]            UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TenantId]      UNIQUEIDENTIFIER  NOT NULL,
    [TransactionId] NVARCHAR(100)     NOT NULL,
    [Payload]       NVARCHAR(MAX)     NOT NULL,
    [Published]     BIT               NOT NULL DEFAULT 0,
    [PublishedAt]   DATETIMEOFFSET    NULL,
    [CreatedAt]     DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt]     DATETIMEOFFSET    NULL,

    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_OutboxMessages_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE NO ACTION,

    -- Primary read pattern: relay polls for unpublished rows ordered by creation time
    INDEX [IX_OutboxMessages_Published_CreatedAt] NONCLUSTERED ([Published], [CreatedAt]),
    INDEX [IX_OutboxMessages_TenantId]            NONCLUSTERED ([TenantId])
);
GO
