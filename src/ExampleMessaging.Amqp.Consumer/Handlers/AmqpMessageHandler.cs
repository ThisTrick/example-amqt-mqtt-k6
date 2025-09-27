using ExampleMessaging.Shared.Models;
using ExampleMessaging.Amqp.Consumer.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExampleMessaging.Amqp.Consumer.Handlers;

/// <summary>
/// Handler for processing AMQP messages
/// </summary>
public class AmqpMessageHandler : IAmqpMessageHandler
{
    private readonly ILogger<AmqpMessageHandler> _logger;

    public AmqpMessageHandler(ILogger<AmqpMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HandleAsync(AmqpMessage message, string routingKey)
    {
        try
        {
            _logger.LogInformation("Processing AMQP message {MessageId} with routing key {RoutingKey}", 
                message.Id, routingKey);

            // Validate message
            if (!message.IsValid())
            {
                _logger.LogWarning("Invalid message {MessageId}: validation failed", message.Id);
                return false;
            }

            // Process based on message type and routing key
            var processingResult = await ProcessMessageAsync(message, routingKey);
            
            if (processingResult)
            {
                _logger.LogInformation("Successfully processed AMQP message {MessageId} of type {MessageType}", 
                    message.Id, message.MessageType);
            }
            else
            {
                _logger.LogWarning("Failed to process AMQP message {MessageId}", message.Id);
            }

            return processingResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AMQP message {MessageId}", message.Id);
            return false;
        }
    }

    private async Task<bool> ProcessMessageAsync(AmqpMessage message, string routingKey)
    {
        // Simulate processing delay
        await Task.Delay(100);

        try
        {
            // Route message based on routing key patterns
            if (routingKey.StartsWith("message."))
            {
                return await ProcessGenericMessage(message, routingKey);
            }
            else if (routingKey.StartsWith("event."))
            {
                return await ProcessEventMessage(message, routingKey);
            }
            else if (routingKey.StartsWith("command."))
            {
                return await ProcessCommandMessage(message, routingKey);
            }
            else if (routingKey.StartsWith("notification."))
            {
                return await ProcessNotificationMessage(message, routingKey);
            }
            else
            {
                _logger.LogWarning("Unknown routing pattern {RoutingKey} for message {MessageId}", 
                    routingKey, message.Id);
                return await ProcessUnknownMessage(message, routingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message processing logic for {MessageId}", message.Id);
            return false;
        }
    }

    private async Task<bool> ProcessGenericMessage(AmqpMessage message, string routingKey)
    {
        _logger.LogInformation("Processing generic message {MessageId}: {Payload}", 
            message.Id, message.Payload);

        // Example processing: Log message details
        _logger.LogDebug("Message details - Exchange: {Exchange}, RoutingKey: {RoutingKey}, Priority: {Priority}, Persistent: {Persistent}",
            message.Exchange, message.RoutingKey, message.Priority, message.Persistent);

        // Simulate business logic
        await SimulateBusinessLogic(message);

        return true;
    }

    private async Task<bool> ProcessEventMessage(AmqpMessage message, string routingKey)
    {
        _logger.LogInformation("Processing event message {MessageId} with routing {RoutingKey}", 
            message.Id, routingKey);

        try
        {
            // Parse event payload
            var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            if (eventData != null)
            {
                _logger.LogDebug("Event data keys: {Keys}", string.Join(", ", eventData.Keys));
            }

            // Simulate event processing
            await SimulateEventProcessing(message, eventData);

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse event payload for message {MessageId}", message.Id);
            return false;
        }
    }

    private async Task<bool> ProcessCommandMessage(AmqpMessage message, string routingKey)
    {
        _logger.LogInformation("Processing command message {MessageId} with routing {RoutingKey}", 
            message.Id, routingKey);

        // Commands require more strict processing
        if (message.MessageType != MessageType.Command)
        {
            _logger.LogWarning("Message {MessageId} has routing key {RoutingKey} but type is {MessageType}", 
                message.Id, routingKey, message.MessageType);
        }

        // Simulate command execution
        await SimulateCommandExecution(message);

        return true;
    }

    private async Task<bool> ProcessNotificationMessage(AmqpMessage message, string routingKey)
    {
        _logger.LogInformation("Processing notification message {MessageId} with routing {RoutingKey}", 
            message.Id, routingKey);

        // Notifications are typically fire-and-forget
        await SimulateNotificationDelivery(message);

        return true;
    }

    private async Task<bool> ProcessUnknownMessage(AmqpMessage message, string routingKey)
    {
        _logger.LogWarning("Processing unknown message type {MessageId} with routing {RoutingKey}", 
            message.Id, routingKey);

        // Still try to process, but with minimal logic
        await Task.Delay(50); // Minimal processing

        return true;
    }

    private async Task SimulateBusinessLogic(AmqpMessage message)
    {
        // Simulate different processing times based on message priority
        var delay = message.Priority switch
        {
            >= 200 => 50,  // High priority - fast processing
            >= 100 => 100, // Medium priority
            _ => 200       // Low priority - slower processing
        };

        await Task.Delay(delay);

        _logger.LogDebug("Completed business logic for message {MessageId} (priority: {Priority})", 
            message.Id, message.Priority);
    }

    private async Task SimulateEventProcessing(AmqpMessage message, Dictionary<string, object>? eventData)
    {
        await Task.Delay(150);

        _logger.LogDebug("Processed event {MessageId} with {DataCount} data fields", 
            message.Id, eventData?.Count ?? 0);
    }

    private async Task SimulateCommandExecution(AmqpMessage message)
    {
        // Commands take longer to process
        await Task.Delay(300);

        _logger.LogDebug("Executed command {MessageId}", message.Id);
    }

    private async Task SimulateNotificationDelivery(AmqpMessage message)
    {
        // Notifications are quick
        await Task.Delay(75);

        _logger.LogDebug("Delivered notification {MessageId}", message.Id);
    }
}
