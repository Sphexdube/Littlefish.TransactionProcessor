if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL")) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL")))
{
    // Local default so AppHost can start without requiring external OTLP env configuration.
    Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:4318");
    Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
}

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server SA password — set in appsettings.Development.json (Parameters:sql-password).
// Override via AppHost user-secrets or the Parameters__sql-password env var for non-local environments.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sql", password: sqlPassword, port: 1433);

// Resource name "TransactionDb" matches GetConnectionString("TransactionDb") in the API and Worker.
// databaseName "dbTransactionProcessor" is the actual SQL Server database Grate creates.
var transactionDb = sqlServer.AddDatabase("TransactionDb", databaseName: "dbTransactionProcessor");

// Grate migration container — runs once after SQL Server is ready, before the .NET services start.
// host.docker.internal lets the Grate container reach the host-mapped SQL Server port.
string dbScriptsPath = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "dbTransactionProcessor"));

// Read the password from AppHost configuration so it is not hardcoded in source.
string dbPassword = builder.Configuration["Parameters:sql-password"]
    ?? throw new InvalidOperationException(
        "Parameters:sql-password is not configured. " +
        "Add it to appsettings.Development.json or set the Parameters__sql-password environment variable.");

var grate = builder.AddContainer("grate-migration", "erikbra/grate")
    .WithBindMount(dbScriptsPath, "/db", isReadOnly: true)
    .WithEntrypoint("/bin/sh")
    .WithArgs(
        "-c",
        "retry_count=0; max_retries=5; " +
        "while [ $retry_count -lt $max_retries ]; do " +
        "  echo \"Attempt $((retry_count + 1))...\"; " +
        "  ./grate " +
        $"    --connstring=\"Server=host.docker.internal,1433;Database=dbTransactionProcessor;User Id=sa;Password={dbPassword};TrustServerCertificate=True\" " +
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

// Azure Service Bus emulator — runs locally via Docker, no Azure subscription needed.
// Queue "transactions-ingest" is defined in ServiceBus.Emulator.Config.json.
var serviceBus = builder
    .AddAzureServiceBus("messaging")
    .RunAsEmulator(emulator =>
    {
        emulator.WithConfigurationFile(
            Path.Combine(builder.AppHostDirectory, "ServiceBus.Emulator.Config.json"));
    });

builder.AddProject<Projects.Transaction_Presentation_Api>("transaction-api")
    .WithReference(transactionDb)
    .WaitFor(grate);

builder.AddProject<Projects.Transaction_Worker_OutboxRelay>("outbox-relay")
    .WithReference(transactionDb)
    .WithReference(serviceBus)
    .WaitFor(grate)
    .WaitFor(serviceBus);

builder.AddProject<Projects.Transaction_Worker_Processor>("transaction-worker")
    .WithReference(transactionDb)
    .WithReference(serviceBus)
    .WaitFor(grate)
    .WaitFor(serviceBus);

builder.Build().Run();
