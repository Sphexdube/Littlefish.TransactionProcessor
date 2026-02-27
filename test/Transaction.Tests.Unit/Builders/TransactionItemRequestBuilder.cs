using Transaction.Application.Models.Request.V1;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.Builders;

public sealed class TransactionItemRequestBuilder : BuilderBase<TransactionItemRequest>
{
    public TransactionItemRequestBuilder() : base(GetDefault()) { }

    public TransactionItemRequestBuilder(string transactionId) : base(GetDefault(transactionId)) { }

    private static TransactionItemRequest GetDefault(string? transactionId = null) => new()
    {
        TransactionId = transactionId ?? DefaultTestIds.TransactionId,
        OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        Amount = 1500.00m,
        Currency = "ZAR",
        MerchantId = DefaultTestIds.MerchantId,
        Type = "PURCHASE",
        OriginalTransactionId = null,
        Metadata = null
    };

    public TransactionItemRequestBuilder WithTransactionId(string transactionId) =>
        (TransactionItemRequestBuilder)With(x => x.TransactionId, transactionId);

    public TransactionItemRequestBuilder WithAmount(decimal amount) =>
        (TransactionItemRequestBuilder)With(x => x.Amount, amount);

    public TransactionItemRequestBuilder WithCurrency(string currency) =>
        (TransactionItemRequestBuilder)With(x => x.Currency, currency);

    public TransactionItemRequestBuilder WithMerchantId(string merchantId) =>
        (TransactionItemRequestBuilder)With(x => x.MerchantId, merchantId);

    public TransactionItemRequestBuilder WithType(string type) =>
        (TransactionItemRequestBuilder)With(x => x.Type, type);

    public TransactionItemRequestBuilder WithOriginalTransactionId(string? originalTransactionId) =>
        (TransactionItemRequestBuilder)With(x => x.OriginalTransactionId, originalTransactionId);

    public TransactionItemRequestBuilder WithOccurredAt(DateTimeOffset occurredAt) =>
        (TransactionItemRequestBuilder)With(x => x.OccurredAt, occurredAt);

    public TransactionItemRequestBuilder WithMetadata(Dictionary<string, string>? metadata) =>
        (TransactionItemRequestBuilder)With(x => x.Metadata, metadata);
}
