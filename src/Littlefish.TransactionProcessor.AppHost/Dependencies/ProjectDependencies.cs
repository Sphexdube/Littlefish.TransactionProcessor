namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class ProjectDependencies
{
    internal static void AddProjectDependencies(
        this IDistributedApplicationBuilder builder, AppHostResources resources)
    {
        string otlpEndpoint = Environment.GetEnvironmentVariable(AppHostConstants.EnvOtelExporterOtlpEndpoint) ?? string.Empty;

        builder.AddProject<Projects.Transaction_Presentation_Api>("transaction-api")
            .WithReference(resources.TransactionDb)
            .WaitFor(resources.GrateMigration)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpEndpoint, otlpEndpoint)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpProtocol, AppHostConstants.OtlpProtocolGrpc)
            .WithHttpHealthCheck("/health");

        builder.AddProject<Projects.Transaction_Worker_OutboxRelay>("outbox-relay")
            .WithReference(resources.TransactionDb)
            .WithReference(resources.ServiceBus)
            .WaitFor(resources.GrateMigration)
            .WaitFor(resources.ServiceBus)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpEndpoint, otlpEndpoint)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpProtocol, AppHostConstants.OtlpProtocolGrpc)
            .WithHttpHealthCheck("/health");

        builder.AddProject<Projects.Transaction_Worker_Processor>("transaction-worker")
            .WithReference(resources.TransactionDb)
            .WithReference(resources.ServiceBus)
            .WaitFor(resources.GrateMigration)
            .WaitFor(resources.ServiceBus)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpEndpoint, otlpEndpoint)
            .WithEnvironment(AppHostConstants.EnvOtelExporterOtlpProtocol, AppHostConstants.OtlpProtocolGrpc)
            .WithHttpHealthCheck("/health");
    }
}
