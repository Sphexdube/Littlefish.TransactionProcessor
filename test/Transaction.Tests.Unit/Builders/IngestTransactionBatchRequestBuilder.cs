using Transaction.Application.Models.Request.V1;

namespace Transaction.Tests.Unit.Builders;

public sealed class IngestTransactionBatchRequestBuilder : BuilderBase<IngestTransactionBatchRequest>
{
    public IngestTransactionBatchRequestBuilder() : base(GetDefault()) { }

    public IngestTransactionBatchRequestBuilder(int transactionCount) : base(GetDefault(transactionCount)) { }

    private static IngestTransactionBatchRequest GetDefault(int count = 100) => new()
    {
        Transactions = GenerateTransactions(count)
    };

    public IngestTransactionBatchRequestBuilder WithTransactions(IList<TransactionItemRequest> transactions) =>
        (IngestTransactionBatchRequestBuilder)With(x => x.Transactions, transactions);

    public static IList<TransactionItemRequest> GenerateTransactions(int count)
    {
        List<TransactionItemRequest> transactions = new();

        for (int i = 0; i < count; i++)
        {
            transactions.Add(new TransactionItemRequestBuilder($"TXN-{i:D6}").Build());
        }

        return transactions;
    }
}
