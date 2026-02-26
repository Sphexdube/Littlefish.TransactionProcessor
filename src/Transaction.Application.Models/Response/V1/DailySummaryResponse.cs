namespace Transaction.Application.Models.Response.V1;

public sealed record DailySummaryResponse
{
    public required string MerchantId { get; init; }

    public required DateOnly Date { get; init; }

    public required decimal TotalAmount { get; init; }

    public required int TransactionCount { get; init; }

    public required DateTimeOffset LastCalculatedAt { get; init; }
}
