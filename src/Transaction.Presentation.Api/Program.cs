using Littlefish.TransactionProcessor.ServiceDefaults;
using Transaction.Presentation.Api.Dependencies;
using Transaction.Presentation.Api.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSwaggerDependencies();
builder.Services.AddDatabaseDependencies(builder.Configuration);
builder.Services.AddObservabilityDependencies();
builder.Services.AddApplicationDependencies();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.UseSwaggerDependencies();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
