using Littlefish.TransactionProcessor.AppHost.Dependencies;

string dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

TelemetrySetup.ConfigureOtlpEnvironment(dotnetEnvironment);
TelemetrySetup.ForwardOtlpToChildServices();

var builder = DistributedApplication.CreateBuilder(args);

builder.AddKeyVaultConfiguration(dotnetEnvironment);

AppHostResources resources = builder.AddInfrastructureDependencies();
builder.AddProjectDependencies(resources);

builder.Build().Run();
