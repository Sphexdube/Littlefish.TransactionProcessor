using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Littlefish.TransactionProcessor.AppHost.Dependencies;

internal static class KeyVaultDependencies
{
    internal static IDistributedApplicationBuilder AddKeyVaultConfiguration(
        this IDistributedApplicationBuilder builder, string dotnetEnvironment)
    {
        if (string.Equals(dotnetEnvironment, AppHostConstants.EnvironmentLocalhost, StringComparison.OrdinalIgnoreCase))
            return builder;

        string vaultUri = builder.Configuration[AppHostConstants.AzureKeyVaultVaultUriKey]
            ?? throw new InvalidOperationException(
                $"{AppHostConstants.AzureKeyVaultVaultUriKey} is required for environment '{dotnetEnvironment}'. " +
                "Add it to appsettings.{Environment}.json.");

        ((IConfigurationBuilder)builder.Configuration).AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
        return builder;
    }
}
