-- TransactionStatuses lookup table
-- Reference data for transaction status values used in the Transactions table
--
-- Values:
--   1 = Received
--   2 = Processing
--   3 = Processed
--   4 = Rejected
--   5 = Review

CREATE TABLE [dbo].[TransactionStatuses]
(
    [Id]          INT            NOT NULL,
    [Name]        NVARCHAR(50)   NOT NULL,
    [Description] NVARCHAR(200)  NULL,
    [SortOrder]   INT            NOT NULL DEFAULT 0,

    CONSTRAINT [PK_TransactionStatuses]      PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_TransactionStatuses_Name] UNIQUE ([Name])
);
GO
