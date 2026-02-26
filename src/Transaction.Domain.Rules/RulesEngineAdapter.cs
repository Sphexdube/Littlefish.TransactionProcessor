using System.Reflection;
using System.Text.Json;
using RulesEngine.Models;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;

namespace Transaction.Domain.Rules;

public sealed class RulesEngineAdapter : IRuleEngine
{
    private const string WorkflowName = "TransactionRules";
    private const string ReviewSuccessEvent = "REVIEW";

    private readonly RulesEngine.RulesEngine _rulesEngine;
    private readonly ITransactionRepository _transactionRepository;

    public RulesEngineAdapter(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
        _rulesEngine = BuildRulesEngine();
    }

    public async Task<IEnumerable<RuleResult>> EvaluateAllAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        bool originalPurchaseExists = false;

        if (context.Transaction.Type == TransactionType.Refund &&
            !string.IsNullOrWhiteSpace(context.Transaction.OriginalTransactionId))
        {
            originalPurchaseExists = await _transactionRepository.ExistsByTransactionIdAsync(
                context.Transaction.TenantId,
                context.Transaction.OriginalTransactionId,
                cancellationToken);
        }

        TransactionRuleInput input = new TransactionRuleInput
        {
            TransactionType = context.Transaction.Type.ToString(),
            Amount = context.Transaction.Amount,
            OriginalPurchaseExists = originalPurchaseExists,
            DailyMerchantLimit = context.Tenant.DailyMerchantLimit,
            HighValueThreshold = context.Tenant.HighValueThreshold,
            ProjectedDailyTotal = context.CurrentDailyMerchantTotal + context.Transaction.Amount
        };

        List<RuleResultTree> ruleResults = await _rulesEngine.ExecuteAllRulesAsync(WorkflowName, input);

        List<RuleResult> results = new List<RuleResult>();

        foreach (RuleResultTree result in ruleResults)
        {
            if (result.Rule.RuleName == "HighValueReview")
            {
                if (result.IsSuccess)
                {
                    results.Add(RuleResult.NeedsReview("Transaction amount exceeds high-value threshold and requires review"));
                }
            }
            else
            {
                if (!result.IsSuccess)
                {
                    results.Add(RuleResult.Failure(result.Rule.ErrorMessage ?? result.Rule.RuleName));
                    break;
                }

                results.Add(RuleResult.Success());
            }
        }

        return results;
    }

    private static RulesEngine.RulesEngine BuildRulesEngine()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"{assembly.GetName().Name}.Rules.TransactionRules.json";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                "Ensure TransactionRules.json is marked as EmbeddedResource in the csproj.");
        }

        using StreamReader reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        Workflow[] workflows = JsonSerializer.Deserialize<Workflow[]>(json)
            ?? throw new InvalidOperationException("Failed to deserialise TransactionRules.json.");

        return new RulesEngine.RulesEngine(workflows);
    }
}
