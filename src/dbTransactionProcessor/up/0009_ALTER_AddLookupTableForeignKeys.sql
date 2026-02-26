-- Adds foreign key constraints from Transactions and Batches to lookup tables.
-- Run after 0005/0006/0007 lookup tables and the seed data have been applied.

ALTER TABLE [dbo].[Transactions]
    ADD CONSTRAINT [FK_Transactions_TransactionTypes]    FOREIGN KEY ([Type])   REFERENCES [dbo].[TransactionTypes]    ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_TransactionStatuses] FOREIGN KEY ([Status]) REFERENCES [dbo].[TransactionStatuses] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[Batches]
    ADD CONSTRAINT [FK_Batches_BatchStatuses] FOREIGN KEY ([Status]) REFERENCES [dbo].[BatchStatuses] ([Id]) ON DELETE NO ACTION;
GO
