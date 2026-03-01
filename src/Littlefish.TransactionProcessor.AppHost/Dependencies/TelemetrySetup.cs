using Microsoft.Extensions.Configuration;

namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class TelemetrySetup
{
    internal static void ConfigureOtlpEnvironment(string dotnetEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL")))
            return;

        IConfiguration preConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{dotnetEnvironment}.json", optional: true)
            .Build();

        string? otlpEndpoint = preConfig["ApplicationConfiguration:Telemetry:OtlpEndpoint"];
        string? otlpHttpEndpoint = preConfig["ApplicationConfiguration:Telemetry:OtlpHttpEndpoint"];
        bool allowUnsecured = bool.TryParse(
            preConfig["ApplicationConfiguration:Telemetry:AllowUnsecuredTransport"], out bool u) && u;

        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            return;

        Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", otlpEndpoint);
        Environment.SetEnvironmentVariable(
            "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", otlpHttpEndpoint ?? otlpEndpoint);

        if (allowUnsecured)
            Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    }

    internal static void ForwardOtlpToChildServices()
    {
        string? dashboardOtlpEndpoint = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL");
        if (string.IsNullOrWhiteSpace(dashboardOtlpEndpoint))
            return;

        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", dashboardOtlpEndpoint);
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
    }
}
