CREATE TABLE [dbo].[RuleWorkflows]
(
    [Id]       INT           NOT NULL,
    [Name]     VARCHAR(100)  NOT NULL,
    [IsActive] BIT           NOT NULL CONSTRAINT [DF_RuleWorkflows_IsActive] DEFAULT 1,

    CONSTRAINT [PK_RuleWorkflows] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_RuleWorkflows_Name] UNIQUE ([Name])
);
GO
