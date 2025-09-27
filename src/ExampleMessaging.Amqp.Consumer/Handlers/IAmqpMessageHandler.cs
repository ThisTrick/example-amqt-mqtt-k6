using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Amqp.Consumer.Handlers;

/// <summary>
/// Interface for handling AMQP messages
/// </summary>
public interface IAmqpMessageHandler
{
    Task<bool> HandleAsync(AmqpMessage message, string routingKey);
}
