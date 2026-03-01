using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Littlefish.TransactionProcessor.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddMeter("Littlefish.TransactionProcessor");
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Prefer Aspire dashboard endpoint when present; this avoids IDE-level OTEL
        // environment overrides redirecting telemetry away from Aspire.
        var otlpEndpoint =
            builder.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            builder.Services.AddOpenTelemetry()
                .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otlpEndpoint));
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Adds Azure Key Vault as a configuration source for all non-Localhost environments.
    /// Localhost reads directly from appsettings.Localhost.json.
    /// All other environments (Development, Test, Production) read secrets from the vault
    /// whose URI is defined in appsettings.{Environment}.json under AzureKeyVault:VaultUri.
    /// Azure Key Vault secret naming: replace ':' with '--' (e.g. ConnectionStrings--TransactionDb).
    /// </summary>
    public static TBuilder AddKeyVaultConfiguration<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (builder.Environment.IsEnvironment("Localhost"))
        {
            return builder;
        }

        string vaultUri = builder.Configuration["AzureKeyVault:VaultUri"]
            ?? throw new InvalidOperationException(
                $"AzureKeyVault:VaultUri is required for environment '{builder.Environment.EnvironmentName}'. " +
                "Add it to appsettings.{Environment}.json.");

        ((IConfigurationBuilder)builder.Configuration).AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
