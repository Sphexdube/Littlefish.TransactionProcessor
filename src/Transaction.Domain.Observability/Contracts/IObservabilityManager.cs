namespace Transaction.Domain.Observability.Contracts;

public interface IObservabilityManager
{
    ILogBuilder LogMessage(string message);
}
