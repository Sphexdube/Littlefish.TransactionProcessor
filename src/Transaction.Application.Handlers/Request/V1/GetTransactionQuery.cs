namespace Transaction.Application.Handlers.Request.V1;

public record GetTransactionQuery(Guid TenantId, string TransactionId);
