using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

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
