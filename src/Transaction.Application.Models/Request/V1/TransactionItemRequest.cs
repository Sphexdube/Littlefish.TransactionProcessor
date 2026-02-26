namespace Transaction.Application.Models.Request.V1;

public sealed record TransactionItemRequest
{
    public required string TransactionId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required string MerchantId { get; init; }

    public required string Type { get; init; }

    public string? OriginalTransactionId { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}
