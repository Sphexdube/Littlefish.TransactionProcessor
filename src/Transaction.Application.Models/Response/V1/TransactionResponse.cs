namespace Transaction.Application.Models.Response.V1;

public record TransactionResponse(
    Guid Id,
    string TransactionId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Type,
    string Status,
    string? OriginalTransactionId,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string? RejectionReason,
    Dictionary<string, string>? Metadata);
