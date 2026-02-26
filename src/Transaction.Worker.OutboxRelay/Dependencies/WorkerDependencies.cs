using Transaction.Worker.OutboxRelay.Workers;

namespace Transaction.Worker.OutboxRelay.Dependencies;

internal static class WorkerDependencies
{
    internal static IServiceCollection AddWorkerDependencies(this IServiceCollection services)
    {
        services.AddHostedService<OutboxRelayWorker>();

        return services;
    }
}
