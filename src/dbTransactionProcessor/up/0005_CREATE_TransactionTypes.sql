-- TransactionTypes lookup table
-- Reference data for transaction type values used in the Transactions table
--
-- Values:
--   1 = Purchase
--   2 = Refund
--   3 = Reversal

CREATE TABLE [dbo].[TransactionTypes]
(
    [Id]          INT            NOT NULL,
    [Name]        NVARCHAR(50)   NOT NULL,
    [Description] NVARCHAR(200)  NULL,
    [SortOrder]   INT            NOT NULL DEFAULT 0,

    CONSTRAINT [PK_TransactionTypes]      PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_TransactionTypes_Name] UNIQUE ([Name])
);
GO
