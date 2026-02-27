namespace Transaction.Worker.Processor;

internal static class ServiceBusReasons
{
    public const string DeserializationFailed = "DeserializationFailed";
    public const string DeserializationFailedDescription = "Payload could not be deserialised.";

    public const string NotFound = "NotFound";
    public const string NotFoundDescription = "TransactionRecord not found in database.";

    public const string TenantNotFound = "TenantNotFound";
    public const string TenantNotFoundDescriptionFormat = "Tenant {0} not found.";

    public const string UnexpectedError = "UnexpectedError";
}
