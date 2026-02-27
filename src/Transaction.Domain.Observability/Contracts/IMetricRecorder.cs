namespace Transaction.Domain.Observability.Contracts;

public interface IMetricRecorder
{
    void Increment(string name, long value = 1);
    void RecordDuration(string name, long milliseconds);
}
