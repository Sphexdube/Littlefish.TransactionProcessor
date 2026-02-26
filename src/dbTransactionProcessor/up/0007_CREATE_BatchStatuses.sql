-- BatchStatuses lookup table
-- Reference data for batch status values used in the Batches table
--
-- Values:
--   1 = Received
--   2 = Processing
--   3 = Completed
--   4 = PartiallyCompleted
--   5 = Failed

CREATE TABLE [dbo].[BatchStatuses]
(
    [Id]          INT            NOT NULL,
    [Name]        NVARCHAR(50)   NOT NULL,
    [Description] NVARCHAR(200)  NULL,
    [SortOrder]   INT            NOT NULL DEFAULT 0,

    CONSTRAINT [PK_BatchStatuses]      PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_BatchStatuses_Name] UNIQUE ([Name])
);
GO
