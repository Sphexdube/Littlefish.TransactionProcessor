using Aspire.Hosting.Azure;

namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal sealed record AppHostResources(
    IResourceBuilder<SqlServerDatabaseResource> TransactionDb,
    IResourceBuilder<AzureServiceBusResource> ServiceBus,
    IResourceBuilder<ContainerResource> GrateMigration);
