using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Worker.OutboxRelay.Workers;

namespace Transaction.Worker.OutboxRelay.Dependencies;

internal static class WorkerDependencies
{
    internal static IServiceCollection AddWorkerDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IObservabilityManager>(sp =>
            new ObservabilityManager(sp.GetRequiredService<ILogger<ObservabilityManager>>()));

        services.AddSingleton<IMetricRecorder, MetricRecorder>();

        services.AddHostedService<OutboxRelayWorker>();

        return services;
    }
}
