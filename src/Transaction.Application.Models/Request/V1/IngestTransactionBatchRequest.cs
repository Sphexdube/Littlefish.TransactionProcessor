namespace Transaction.Application.Models.Request.V1;

public record IngestTransactionBatchRequest(IList<TransactionItemRequest> Transactions);
