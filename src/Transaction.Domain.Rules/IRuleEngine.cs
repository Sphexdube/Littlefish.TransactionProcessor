namespace Transaction.Domain.Rules;

public interface IRuleEngine
{
    Task<IEnumerable<RuleResult>> EvaluateAllAsync(RuleContext context, CancellationToken cancellationToken = default);
}
