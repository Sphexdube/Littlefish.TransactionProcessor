using Transaction.Domain.Observability.Contracts;

namespace Transaction.Presentation.Api.Observability;

public sealed class ObservabilityManager(ILogger logger) : IObservabilityManager
{
    public ILogBuilder LogMessage(string message) => new LogBuilder(logger, message);
}

internal sealed class LogBuilder(ILogger logger, string message) : ILogBuilder
{
    public void AsInfo()    => logger.LogInformation("{Message}", message);
    public void AsWarning() => logger.LogWarning("{Message}", message);
    public void AsError()   => logger.LogError("{Message}", message);
}
