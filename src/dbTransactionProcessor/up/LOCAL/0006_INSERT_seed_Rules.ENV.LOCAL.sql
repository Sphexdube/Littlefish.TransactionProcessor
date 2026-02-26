-- Seed Rules for local development
-- WorkflowId 1 = 'TransactionRules'
INSERT INTO [dbo].[Rules] ([Id], [WorkflowId], [RuleName], [RuleExpressionType], [Expression], [ErrorMessage], [SuccessEvent], [SortOrder], [IsActive])
VALUES
    (1, 1, 'NegativePurchaseAmount',
        'LambdaExpression',
        'input1.TransactionType != "Purchase" || input1.Amount > 0',
        'PURCHASE amount cannot be negative',
        NULL, 1, 1),

    (2, 1, 'RefundRequiresOriginalPurchase',
        'LambdaExpression',
        'input1.TransactionType != "Refund" || input1.OriginalPurchaseExists == true',
        'REFUND must reference an existing PURCHASE transaction',
        NULL, 2, 1),

    (3, 1, 'DailyMerchantLimit',
        'LambdaExpression',
        'input1.TransactionType != "Purchase" || input1.ProjectedDailyTotal <= input1.DailyMerchantLimit',
        'Daily merchant purchase limit would be exceeded',
        NULL, 3, 1),

    (4, 1, 'HighValueReview',
        'LambdaExpression',
        'input1.Amount > input1.HighValueThreshold',
        NULL,
        'REVIEW', 4, 1);
GO
