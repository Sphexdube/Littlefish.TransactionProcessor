using Littlefish.TransactionProcessor.ServiceDefaults;
using Transaction.Presentation.Api.Dependencies;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

ControllerRegistration.Register(builder.Services);
DependencyRegistration.Register(builder.Services, builder.Configuration);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
MiddlewareRegistration.Register(app);
EndpointRegistration.Register(app);
FeatureRegistration.Register(app, app.Environment);

app.Run();

public partial class Program { }
