namespace Transaction.Domain.Entities.Enums;

public enum BatchStatus
{
    Received = 1,
    Processing,
    Completed,
    PartiallyCompleted,
    Failed
}
