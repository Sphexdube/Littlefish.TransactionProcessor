namespace Transaction.Domain.Rules;

public class RuleEngine : IRuleEngine
{
    private readonly IEnumerable<IBusinessRule> _rules;

    public RuleEngine(IEnumerable<IBusinessRule> rules)
    {
        _rules = rules.OrderBy(r => r.Order);
    }

    public async Task<IEnumerable<RuleResult>> EvaluateAllAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<RuleResult>();

        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context, cancellationToken);
            results.Add(result);

            if (!result.IsValid)
            {
                break;
            }
        }

        return results;
    }
}
