namespace Transaction.Domain.Commands;

public sealed record GetDailySummaryQuery
{
    public required Guid TenantId { get; init; }

    public required string MerchantId { get; init; }

    public required DateOnly Date { get; init; }
}
