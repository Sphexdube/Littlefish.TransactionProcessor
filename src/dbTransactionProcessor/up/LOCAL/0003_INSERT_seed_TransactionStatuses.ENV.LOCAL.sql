-- Seed TransactionStatuses lookup table for local development
INSERT INTO [dbo].[TransactionStatuses] ([Id], [Name], [Description], [SortOrder])
VALUES
    (1, 'Received',   'Transaction received and queued for processing',                        1),
    (2, 'Processing', 'Transaction is currently being evaluated by the rule engine',           2),
    (3, 'Processed',  'Transaction passed all rules and has been successfully processed',      3),
    (4, 'Rejected',   'Transaction was rejected by one or more business rules',               4),
    (5, 'Review',     'Transaction passed rules but exceeds the high-value review threshold',  5);
GO
