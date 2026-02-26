namespace Transaction.Application.Handlers.Request.V1;

public record GetDailySummaryQuery(Guid TenantId, string MerchantId, DateOnly Date);
