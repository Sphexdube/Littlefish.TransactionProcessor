namespace Transaction.Domain.Rules;

public record RuleResult(bool IsValid, string? ErrorMessage = null, bool RequiresReview = false)
{
    public static RuleResult Success()
    {
        return new(IsValid: true);
    }

    public static RuleResult Failure(string errorMessage)
    {
        return new(IsValid: false, errorMessage);
    }

    public static RuleResult NeedsReview(string reason)
    {
        return new(IsValid: true, reason, RequiresReview: true);
    }
}
