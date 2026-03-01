using Aspire.Hosting.Azure;

namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class InfrastructureDependencies
{
    internal static AppHostResources AddInfrastructureDependencies(
        this IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);
        IResourceBuilder<SqlServerServerResource> sqlServer = builder.AddSqlServer("sql", password: sqlPassword, port: 1433);
        IResourceBuilder<SqlServerDatabaseResource> transactionDb = sqlServer.AddDatabase("TransactionDb", databaseName: "dbTransactionProcessor");

        string dbScriptsPath = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "..", "dbTransactionProcessor"));

        string grateDbServer = builder.Configuration["ApplicationConfiguration:Grate:DatabaseServer"]
            ?? throw new InvalidOperationException("ApplicationConfiguration:Grate:DatabaseServer is not configured.");
        string grateDbName = builder.Configuration["ApplicationConfiguration:Grate:DatabaseName"]
            ?? throw new InvalidOperationException("ApplicationConfiguration:Grate:DatabaseName is not configured.");
        string grateDbUser = builder.Configuration["ApplicationConfiguration:Grate:DatabaseUsername"]
            ?? throw new InvalidOperationException("ApplicationConfiguration:Grate:DatabaseUsername is not configured.");
        string grateDbTrustCert = builder.Configuration["ApplicationConfiguration:Grate:TrustServerCertificate"] ?? "False";
        string grateDbPassword = builder.Configuration["Parameters:sql-password"]
            ?? throw new InvalidOperationException("Parameters:sql-password is not configured.");

        IResourceBuilder<ContainerResource> grate = builder.AddContainer("grate-migration", "erikbra/grate")
            .WithContainerRuntimeArgs("--platform", "linux/amd64")
            .WithBindMount(dbScriptsPath, "/db", isReadOnly: true)
            .WithEntrypoint("/bin/sh")
            .WithArgs(
                "-c",
                "retry_count=0; max_retries=5; " +
                "while [ $retry_count -lt $max_retries ]; do " +
                "  echo \"Attempt $((retry_count + 1))...\"; " +
                "  /app/grate " +
                $"    --connstring='Server={grateDbServer};Database={grateDbName};User Id={grateDbUser};Password={grateDbPassword};TrustServerCertificate={grateDbTrustCert}' " +
                "    --sqlfilesdirectory=/db " +
                "    --version=1.0.0 " +
                "    --databasetype=sqlserver " +
                "    --silent " +
                "    --outputPath=/output " +
                "    --createdatabase=true " +
                "    --environment=LOCAL " +
                "    --transaction=false && echo \"Migrations succeeded.\" && exit 0; " +
                "  retry_count=$((retry_count + 1)); " +
                "  echo \"Retrying in 5 seconds...\"; sleep 5; " +
                "done; " +
                "echo \"Migrations failed after $max_retries attempts.\"; exit 1")
            .WaitFor(sqlServer);

        IResourceBuilder<AzureServiceBusResource> serviceBus = builder
            .AddAzureServiceBus("messaging")
            .RunAsEmulator(emulator =>
            {
                emulator.WithConfigurationFile(
                    Path.Combine(builder.AppHostDirectory, "ServiceBus.Emulator.Config.json"));
            });

        return new AppHostResources(transactionDb, serviceBus, grate);
    }
}
