namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class AppHostConstants
{
    internal const string EnvDotnetDashboardOtlpEndpointUrl = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    internal const string EnvDotnetDashboardOtlpHttpEndpointUrl = "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
    internal const string EnvAspireAllowUnsecuredTransport = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
    internal const string EnvOtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    internal const string EnvOtelExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    internal const string OtlpProtocolGrpc = "grpc";

    internal const string EnvironmentLocalhost = "Localhost";

    internal const string TelemetryOtlpEndpointKey = "ApplicationConfiguration:Telemetry:OtlpEndpoint";
    internal const string TelemetryOtlpHttpEndpointKey = "ApplicationConfiguration:Telemetry:OtlpHttpEndpoint";
    internal const string TelemetryAllowUnsecuredTransportKey = "ApplicationConfiguration:Telemetry:AllowUnsecuredTransport";

    internal const string GrateDatabaseServerKey = "ApplicationConfiguration:Grate:DatabaseServer";
    internal const string GrateDatabaseNameKey = "ApplicationConfiguration:Grate:DatabaseName";
    internal const string GrateDatabaseUsernameKey = "ApplicationConfiguration:Grate:DatabaseUsername";
    internal const string GrateTrustServerCertificateKey = "ApplicationConfiguration:Grate:TrustServerCertificate";
    internal const string SqlPasswordParameterKey = "Parameters:sql-password";

    internal const string AzureKeyVaultVaultUriKey = "AzureKeyVault:VaultUri";
}
