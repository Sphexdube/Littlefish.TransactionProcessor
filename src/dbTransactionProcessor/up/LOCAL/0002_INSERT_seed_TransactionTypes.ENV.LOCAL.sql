-- Seed TransactionTypes lookup table for local development
INSERT INTO [dbo].[TransactionTypes] ([Id], [Name], [Description], [SortOrder])
VALUES
    (1, 'Purchase',  'A standard purchase transaction',          1),
    (2, 'Refund',    'A refund against a prior purchase',        2),
    (3, 'Reversal',  'A full reversal of a prior transaction',   3);
GO
