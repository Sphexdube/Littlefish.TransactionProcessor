using RulesEngine.Models;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;

namespace Transaction.Domain.Rules;

public sealed class RulesEngineAdapter : IRuleEngine
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRuleWorkflowRepository _ruleWorkflowRepository;

    public RulesEngineAdapter(
        ITransactionRepository transactionRepository,
        IRuleWorkflowRepository ruleWorkflowRepository)
    {
        _transactionRepository = transactionRepository;
        _ruleWorkflowRepository = ruleWorkflowRepository;
    }

    public async Task<IEnumerable<RuleResult>> EvaluateAllAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        RuleWorkflow? workflow = await _ruleWorkflowRepository.GetByNameAsync(RuleMessages.TransactionRulesWorkflow, cancellationToken);

        if (workflow == null)
        {
            throw new InvalidOperationException(string.Format(RuleMessages.WorkflowNotFound, RuleMessages.TransactionRulesWorkflow));
        }

        RulesEngine.RulesEngine rulesEngine = BuildRulesEngine(workflow);

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

        List<RuleResultTree> ruleResults = await rulesEngine.ExecuteAllRulesAsync(RuleMessages.TransactionRulesWorkflow, input);

        List<RuleResult> results = new List<RuleResult>();

        foreach (RuleResultTree result in ruleResults)
        {
            if (result.Rule.RuleName == RuleMessages.HighValueReviewRuleName)
            {
                if (result.IsSuccess)
                {
                    results.Add(RuleResult.NeedsReview(RuleMessages.HighValueThresholdReview));
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

    private static RulesEngine.RulesEngine BuildRulesEngine(RuleWorkflow workflow)
    {
        List<Rule> rules = workflow.Rules
            .Select(r => new Rule
            {
                RuleName = r.RuleName,
                RuleExpressionType = Enum.Parse<RuleExpressionType>(r.RuleExpressionType),
                Expression = r.Expression,
                ErrorMessage = r.ErrorMessage,
                SuccessEvent = r.SuccessEvent
            })
            .ToList();

        Workflow[] workflows = new Workflow[]
        {
            new Workflow
            {
                WorkflowName = workflow.Name,
                Rules = rules
            }
        };

        return new RulesEngine.RulesEngine(workflows);
    }
}
