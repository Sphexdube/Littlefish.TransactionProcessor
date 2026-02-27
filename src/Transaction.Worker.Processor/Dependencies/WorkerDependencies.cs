using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Worker.Processor.Workers;

namespace Transaction.Worker.Processor.Dependencies;

internal static class WorkerDependencies
{
    internal static IServiceCollection AddWorkerDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IObservabilityManager>(sp =>
            new ObservabilityManager(sp.GetRequiredService<ILogger<ObservabilityManager>>()));

        services.AddSingleton<IMetricRecorder, MetricRecorder>();

        services.AddHostedService<TransactionProcessingWorker>();

        return services;
    }
}
