using Azure.Messaging.ServiceBus;

namespace Transaction.Infrastructure.Messaging;

public sealed class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly ServiceBusClient _client;

    public ServiceBusPublisher(ServiceBusClient client)
    {
        _client = client;
    }

    public async Task PublishAsync(string queueName, string messageBody, CancellationToken cancellationToken = default)
    {
        ServiceBusSender sender = _client.CreateSender(queueName);
        await using ServiceBusSender _ = sender;

        ServiceBusMessage message = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
    }
}
