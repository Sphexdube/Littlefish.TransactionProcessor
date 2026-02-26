namespace Transaction.Application.Models.Response.V1;

public sealed record TransactionResponse
{
    public required Guid Id { get; init; }

    public required string TransactionId { get; init; }

    public required string MerchantId { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }

    public string? OriginalTransactionId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; init; }

    public string? RejectionReason { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}
