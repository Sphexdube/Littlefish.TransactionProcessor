namespace Transaction.Worker.Processor.Dependencies;

internal static class MessagingDependencies
{
    internal static IHostApplicationBuilder AddMessagingDependencies(this IHostApplicationBuilder builder)
    {
        builder.AddAzureServiceBusClient("messaging");

        return builder;
    }
}
