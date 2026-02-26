using Transaction.Infrastructure.Messaging;

namespace Transaction.Worker.OutboxRelay.Dependencies;

internal static class MessagingDependencies
{
    internal static IHostApplicationBuilder AddMessagingDependencies(this IHostApplicationBuilder builder)
    {
        builder.AddAzureServiceBusClient("messaging");
        builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

        return builder;
    }
}
