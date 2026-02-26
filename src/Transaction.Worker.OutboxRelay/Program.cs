using Littlefish.TransactionProcessor.ServiceDefaults;
using Transaction.Worker.OutboxRelay.Dependencies;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDatabaseDependencies(builder.Configuration);
builder.AddMessagingDependencies();
builder.Services.AddWorkerDependencies();

IHost host = builder.Build();
host.Run();
