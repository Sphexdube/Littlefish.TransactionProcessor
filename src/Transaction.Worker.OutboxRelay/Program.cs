using Littlefish.TransactionProcessor.ServiceDefaults;
using Transaction.Worker.OutboxRelay.Dependencies;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeyVaultConfiguration();
builder.Services.AddDatabaseDependencies(builder.Configuration);
builder.AddMessagingDependencies();
builder.Services.AddWorkerDependencies();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
