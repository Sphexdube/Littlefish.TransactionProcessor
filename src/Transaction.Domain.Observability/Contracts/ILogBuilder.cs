namespace Transaction.Domain.Observability.Contracts;

public interface ILogBuilder
{
    void AsInfo();
    void AsWarning();
    void AsError();
}
