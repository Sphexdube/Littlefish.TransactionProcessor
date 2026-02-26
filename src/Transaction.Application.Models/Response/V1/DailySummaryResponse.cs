namespace Transaction.Application.Models.Response.V1;

public record DailySummaryResponse(
    string MerchantId,
    DateOnly Date,
    decimal TotalAmount,
    int TransactionCount,
    DateTimeOffset LastCalculatedAt);
