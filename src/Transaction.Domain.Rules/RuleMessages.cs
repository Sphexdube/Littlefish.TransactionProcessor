namespace Transaction.Domain.Rules;

public static class RuleMessages
{
    // Workflow
    public const string TransactionRulesWorkflow = "TransactionRules";
    public const string WorkflowNotFound = "Rule workflow '{0}' not found in database.";

    // Rule names
    public const string NegativePurchaseAmountRule = "NegativePurchaseAmount";
    public const string RefundRequiresOriginalPurchaseRule = "RefundRequiresOriginalPurchase";
    public const string DailyMerchantLimitRule = "DailyMerchantLimit";
    public const string HighValueReviewRuleName = "HighValueReview";

    // Rule error messages
    public const string NegativePurchaseAmountError = "PURCHASE amount cannot be negative";
    public const string RefundRequiresOriginalPurchaseError = "REFUND must reference an existing PURCHASE transaction";
    public const string DailyMerchantLimitError = "Daily merchant purchase limit would be exceeded";
    public const string HighValueThresholdReview = "Transaction amount exceeds high-value threshold and requires review";

    // Rule expression types and events
    public const string LambdaExpressionType = "LambdaExpression";
    public const string ReviewSuccessEvent = "REVIEW";
}
