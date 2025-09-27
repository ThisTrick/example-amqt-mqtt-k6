using Microsoft.Extensions.Logging;
using ExampleMessaging.Shared.Models;
using System.Text.Json;

namespace ExampleMessaging.Mqtt.Consumer.Handlers;

public class MqttMessageHandler
{
    private readonly ILogger<MqttMessageHandler> _logger;

    public MqttMessageHandler(ILogger<MqttMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing MQTT message {MessageId} from topic {Topic} of type {MessageType}",
                message.Id, message.Topic, message.MessageType);

            // Route message based on topic pattern
            var processed = message.Topic switch
            {
                var t when t.StartsWith("messages/orders") => await HandleOrderMessageAsync(message, cancellationToken),
                var t when t.StartsWith("messages/inventory") => await HandleInventoryMessageAsync(message, cancellationToken),
                var t when t.StartsWith("messages/notifications") => await HandleNotificationMessageAsync(message, cancellationToken),
                var t when t.StartsWith("messages/logs") => await HandleLogMessageAsync(message, cancellationToken),
                _ => await HandleUnknownMessageAsync(message, cancellationToken)
            };

            if (processed)
            {
                _logger.LogDebug("Successfully processed MQTT message {MessageId}", message.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing MQTT message {MessageId} from topic {Topic}: {Error}",
                message.Id, message.Topic, ex.Message);
            throw;
        }
    }

    private async Task<bool> HandleOrderMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order message: {Payload}", message.Payload);
        
        // Simulate order processing
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);
        
        _logger.LogDebug("Order message processed with QoS {QoS}, Retain: {Retain}", 
            message.QosLevel, message.Retain);
        
        return true;
    }

    private async Task<bool> HandleInventoryMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing inventory message: {Payload}", message.Payload);
        
        // Simulate inventory update
        await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
        
        _logger.LogDebug("Inventory message processed with QoS {QoS}", message.QosLevel);
        
        return true;
    }

    private async Task<bool> HandleNotificationMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing notification message: {Payload}", message.Payload);
        
        // Simulate notification delivery
        await Task.Delay(Random.Shared.Next(20, 100), cancellationToken);
        
        _logger.LogDebug("Notification message delivered");
        
        return true;
    }

    private async Task<bool> HandleLogMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing log message: {Payload}", message.Payload);
        
        // Simulate log storage
        await Task.Delay(Random.Shared.Next(10, 50), cancellationToken);
        
        _logger.LogDebug("Log message stored");
        
        return true;
    }

    private async Task<bool> HandleUnknownMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received message on unknown topic {Topic}. Payload: {Payload}", 
            message.Topic, message.Payload);
        
        // Default processing
        await Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
        
        return false;
    }
}
