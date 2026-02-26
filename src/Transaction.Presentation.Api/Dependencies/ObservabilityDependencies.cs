using Transaction.Domain.Observability.Contracts;
using Transaction.Presentation.Api.Observability;

namespace Transaction.Presentation.Api.Dependencies;

internal static class ObservabilityDependencies
{
    internal static IServiceCollection AddObservabilityDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IObservabilityManager>(sp =>
            new ObservabilityManager(sp.GetRequiredService<ILogger<ObservabilityManager>>()));

        return services;
    }
}
