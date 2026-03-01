namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class ProjectDependencies
{
    internal static void AddProjectDependencies(
        this IDistributedApplicationBuilder builder, AppHostResources resources)
    {
        string otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? string.Empty;

        builder.AddProject<Projects.Transaction_Presentation_Api>("transaction-api")
            .WithReference(resources.TransactionDb)
            .WaitFor(resources.GrateMigration)
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
            .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
            .WithHttpHealthCheck("/health");

        builder.AddProject<Projects.Transaction_Worker_OutboxRelay>("outbox-relay")
            .WithReference(resources.TransactionDb)
            .WithReference(resources.ServiceBus)
            .WaitFor(resources.GrateMigration)
            .WaitFor(resources.ServiceBus)
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
            .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
            .WithHttpHealthCheck("/health");

        builder.AddProject<Projects.Transaction_Worker_Processor>("transaction-worker")
            .WithReference(resources.TransactionDb)
            .WithReference(resources.ServiceBus)
            .WaitFor(resources.GrateMigration)
            .WaitFor(resources.ServiceBus)
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
            .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
            .WithHttpHealthCheck("/health");
    }
}
