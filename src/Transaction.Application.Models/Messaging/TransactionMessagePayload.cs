namespace Transaction.Application.Models.Messaging;

public sealed record TransactionMessagePayload
{
    public required Guid TenantId { get; init; }
    public required string TransactionId { get; init; }
    public required string MerchantId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Type { get; init; }
    public string? OriginalTransactionId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public required Guid BatchId { get; init; }
    public required string CorrelationId { get; init; }
}
