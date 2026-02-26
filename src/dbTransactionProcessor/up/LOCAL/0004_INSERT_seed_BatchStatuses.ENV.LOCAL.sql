-- Seed BatchStatuses lookup table for local development
INSERT INTO [dbo].[BatchStatuses] ([Id], [Name], [Description], [SortOrder])
VALUES
    (1, 'Received',           'Batch received and accepted for processing',            1),
    (2, 'Processing',         'Batch is actively being processed by the worker',       2),
    (3, 'Completed',          'All transactions in the batch have been processed',     3),
    (4, 'PartiallyCompleted', 'Some transactions were rejected; others were accepted', 4),
    (5, 'Failed',             'Batch processing failed due to a system error',         5);
GO
