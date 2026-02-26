using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;

namespace Transaction.Domain.Rules.Rules;

public class RefundRequiresOriginalPurchaseRule : IBusinessRule
{
    private readonly ITransactionRepository _transactionRepository;

    public string RuleName => "RefundRequiresOriginalPurchase";

    public int Order => 2;

    public RefundRequiresOriginalPurchaseRule(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.Transaction.Type != TransactionType.Refund)
        {
            return RuleResult.Success();
        }

        if (string.IsNullOrEmpty(context.Transaction.OriginalTransactionId))
        {
            return RuleResult.Failure("REFUND must reference an original transaction");
        }

        var originalTransaction = await _transactionRepository.GetByTransactionIdAsync(
            context.Transaction.TenantId,
            context.Transaction.OriginalTransactionId,
            cancellationToken);

        if (originalTransaction == null)
        {
            return RuleResult.Failure($"Original PURCHASE transaction '{context.Transaction.OriginalTransactionId}' not found");
        }

        if (originalTransaction.Type != TransactionType.Purchase)
        {
            return RuleResult.Failure("REFUND must reference a PURCHASE transaction");
        }

        return RuleResult.Success();
    }
}
