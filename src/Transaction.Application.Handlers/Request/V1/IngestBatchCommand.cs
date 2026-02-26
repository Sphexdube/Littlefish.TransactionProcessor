using Transaction.Application.Models.Request.V1;

namespace Transaction.Application.Handlers.Request.V1;

public record IngestBatchCommand(Guid TenantId, IngestTransactionBatchRequest Request, string CorrelationId);
