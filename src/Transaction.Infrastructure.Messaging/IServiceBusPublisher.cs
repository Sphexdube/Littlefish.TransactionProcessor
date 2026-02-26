namespace Transaction.Infrastructure.Messaging;

public interface IServiceBusPublisher
{
    Task PublishAsync(string queueName, string messageBody, CancellationToken cancellationToken = default);
}
