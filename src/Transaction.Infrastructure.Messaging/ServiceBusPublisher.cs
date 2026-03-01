using Azure.Messaging.ServiceBus;

namespace Transaction.Infrastructure.Messaging;

public sealed class ServiceBusPublisher(ServiceBusClient client) : IServiceBusPublisher
{
    public async Task PublishAsync(string queueName, string messageBody, CancellationToken cancellationToken = default)
    {
        ServiceBusSender sender = client.CreateSender(queueName);
        await using ServiceBusSender _ = sender;

        ServiceBusMessage message = new(messageBody)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
    }
}
