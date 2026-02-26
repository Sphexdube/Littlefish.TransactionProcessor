using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Rules.Rules;

public class NegativePurchaseAmountRule : IBusinessRule
{
    public string RuleName => "NegativePurchaseAmount";

    public int Order => 1;

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.Transaction.Type == TransactionType.Purchase && context.Transaction.Amount < 0m)
        {
            return Task.FromResult(RuleResult.Failure("PURCHASE amount cannot be negative"));
        }

        return Task.FromResult(RuleResult.Success());
    }
}
