using Littlefish.TransactionProcessor.ServiceDefaults;
using Transaction.Worker.Processor.Dependencies;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeyVaultConfiguration();
builder.Services.AddDatabaseDependencies(builder.Configuration);
builder.Services.AddRulesDependencies();
builder.AddMessagingDependencies();
builder.Services.AddWorkerDependencies();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
