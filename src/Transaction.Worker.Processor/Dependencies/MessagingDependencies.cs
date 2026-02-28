using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Transaction.Worker.Processor.Dependencies;

internal static class MessagingDependencies
{
    internal static IHostApplicationBuilder AddMessagingDependencies(this IHostApplicationBuilder builder)
    {
        builder.AddAzureServiceBusClient("messaging");

        // Tag the health check registered by AddAzureServiceBusClient as "ready"
        // so it is included in the /health/ready endpoint polled by Aspire.
        builder.Services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (HealthCheckRegistration registration in options.Registrations)
            {
                if (registration.Name.Contains("ServiceBus", StringComparison.OrdinalIgnoreCase))
                {
                    registration.Tags.Add("ready");
                }
            }
        });

        return builder;
    }
}
