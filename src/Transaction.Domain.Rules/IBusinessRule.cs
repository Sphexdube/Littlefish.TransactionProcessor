namespace Transaction.Domain.Rules;

public interface IBusinessRule
{
    string RuleName { get; }

    int Order { get; }

    Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default);
}
