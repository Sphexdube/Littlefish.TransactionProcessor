namespace Transaction.Application.Models.Request.V1;

public record TransactionItemRequest(
    string TransactionId,
    DateTimeOffset OccurredAt,
    decimal Amount,
    string Currency,
    string MerchantId,
    string Type,
    string? OriginalTransactionId,
    Dictionary<string, string>? Metadata);
