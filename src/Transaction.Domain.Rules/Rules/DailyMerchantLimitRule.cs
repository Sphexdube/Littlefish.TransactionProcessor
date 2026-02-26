using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Rules.Rules;

public class DailyMerchantLimitRule : IBusinessRule
{
    public string RuleName => "DailyMerchantLimit";

    public int Order => 3;

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.Transaction.Type != TransactionType.Purchase)
        {
            return Task.FromResult(RuleResult.Success());
        }

        decimal projectedTotal = context.CurrentDailyMerchantTotal + context.Transaction.Amount;

        if (projectedTotal > context.Tenant.DailyMerchantLimit)
        {
            return Task.FromResult(RuleResult.Failure(
                $"Daily merchant limit of {context.Tenant.DailyMerchantLimit:C} would be exceeded. Current total: {context.CurrentDailyMerchantTotal:C}, Transaction: {context.Transaction.Amount:C}"));
        }

        return Task.FromResult(RuleResult.Success());
    }
}
