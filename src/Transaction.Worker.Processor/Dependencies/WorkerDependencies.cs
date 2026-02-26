using Transaction.Worker.Processor.Workers;

namespace Transaction.Worker.Processor.Dependencies;

internal static class WorkerDependencies
{
    internal static IServiceCollection AddWorkerDependencies(this IServiceCollection services)
    {
        services.AddHostedService<TransactionProcessingWorker>();

        return services;
    }
}
