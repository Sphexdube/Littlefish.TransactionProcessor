using Microsoft.Extensions.Configuration;

namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class TelemetrySetup
{
    internal static void ConfigureOtlpEnvironment(string dotnetEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AppHostConstants.EnvDotnetDashboardOtlpEndpointUrl)))
            return;

        IConfiguration preConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{dotnetEnvironment}.json", optional: true)
            .Build();

        string? otlpEndpoint = preConfig[AppHostConstants.TelemetryOtlpEndpointKey];
        string? otlpHttpEndpoint = preConfig[AppHostConstants.TelemetryOtlpHttpEndpointKey];
        bool allowUnsecured = bool.TryParse(
            preConfig[AppHostConstants.TelemetryAllowUnsecuredTransportKey], out bool u) && u;

        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            return;

        Environment.SetEnvironmentVariable(AppHostConstants.EnvDotnetDashboardOtlpEndpointUrl, otlpEndpoint);
        Environment.SetEnvironmentVariable(
            AppHostConstants.EnvDotnetDashboardOtlpHttpEndpointUrl, otlpHttpEndpoint ?? otlpEndpoint);

        if (allowUnsecured)
            Environment.SetEnvironmentVariable(AppHostConstants.EnvAspireAllowUnsecuredTransport, "true");
    }

    internal static void ForwardOtlpToChildServices()
    {
        string? dashboardOtlpEndpoint = Environment.GetEnvironmentVariable(AppHostConstants.EnvDotnetDashboardOtlpEndpointUrl);
        if (string.IsNullOrWhiteSpace(dashboardOtlpEndpoint))
            return;

        Environment.SetEnvironmentVariable(AppHostConstants.EnvOtelExporterOtlpEndpoint, dashboardOtlpEndpoint);
        Environment.SetEnvironmentVariable(AppHostConstants.EnvOtelExporterOtlpProtocol, AppHostConstants.OtlpProtocolGrpc);
    }
}
