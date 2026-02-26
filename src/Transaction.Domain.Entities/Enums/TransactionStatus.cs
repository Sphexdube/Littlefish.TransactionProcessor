namespace Transaction.Domain.Entities.Enums;

public enum TransactionStatus
{
    Received = 1,
    Processing,
    Processed,
    Rejected,
    Review
}
