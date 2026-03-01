namespace Transaction.Tests.Unit.Models;

public static class DefaultTestIds
{
    public static Guid TenantId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    public static string MerchantId { get; } = "MERCHANT001";

    public static string CorrelationId { get; } = "CORR-TEST-001";

    public static string TransactionId { get; } = "TXN-TEST-001";
}
