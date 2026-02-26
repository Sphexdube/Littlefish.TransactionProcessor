CREATE TABLE [dbo].[Rules]
(
    [Id]                   INT              NOT NULL,
    [WorkflowId]           INT              NOT NULL,
    [RuleName]             VARCHAR(100)     NOT NULL,
    [RuleExpressionType]   VARCHAR(50)      NOT NULL CONSTRAINT [DF_Rules_RuleExpressionType] DEFAULT 'LambdaExpression',
    [Expression]           NVARCHAR(MAX)    NOT NULL,
    [ErrorMessage]         NVARCHAR(500)    NULL,
    [SuccessEvent]         VARCHAR(100)     NULL,
    [SortOrder]            INT              NOT NULL CONSTRAINT [DF_Rules_SortOrder] DEFAULT 0,
    [IsActive]             BIT              NOT NULL CONSTRAINT [DF_Rules_IsActive] DEFAULT 1,

    CONSTRAINT [PK_Rules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rules_RuleWorkflows] FOREIGN KEY ([WorkflowId]) REFERENCES [dbo].[RuleWorkflows] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Rules_WorkflowId_RuleName] UNIQUE ([WorkflowId], [RuleName])
);
GO

CREATE INDEX [IX_Rules_WorkflowId_IsActive] ON [dbo].[Rules] ([WorkflowId], [IsActive]);
GO
