namespace Transaction.Domain.Rules.Rules;

public class HighValueReviewRule : IBusinessRule
{
    public string RuleName => "HighValueReview";

    public int Order => 4;

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.Transaction.Amount > context.Tenant.HighValueThreshold)
        {
            return Task.FromResult(RuleResult.NeedsReview(
                $"Transaction amount {context.Transaction.Amount:C} exceeds review threshold of {context.Tenant.HighValueThreshold:C}"));
        }

        return Task.FromResult(RuleResult.Success());
    }
}
