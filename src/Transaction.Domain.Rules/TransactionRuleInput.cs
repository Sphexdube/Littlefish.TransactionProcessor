namespace Transaction.Domain.Rules;

public sealed record TransactionRuleInput
{
    public required string TransactionType { get; init; }
    public required decimal Amount { get; init; }
    public required bool OriginalPurchaseExists { get; init; }
    public required decimal DailyMerchantLimit { get; init; }
    public required decimal HighValueThreshold { get; init; }
    public required decimal ProjectedDailyTotal { get; init; }
}
