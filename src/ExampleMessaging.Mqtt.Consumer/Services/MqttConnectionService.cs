using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExampleMessaging.Shared.Configuration;
using ExampleMessaging.Shared.Models;
using ExampleMessaging.Mqtt.Consumer.Handlers;

namespace ExampleMessaging.Mqtt.Consumer.Services;

public class MqttConnectionService : BackgroundService
{
    private readonly ILogger<MqttConnectionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttConsumerConfig _config;

    public MqttConnectionService(
        ILogger<MqttConnectionService> logger,
        IServiceProvider serviceProvider,
        IOptions<MqttConsumerConfig> config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Consumer service starting...");

        try
        {
            await ConnectAsync(stoppingToken);
            await SubscribeToTopicsAsync(stoppingToken);
            await SimulateMessageReceptionAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MQTT Consumer service stopping due to cancellation...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MQTT Consumer service");
            throw;
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to MQTT broker at {HostName}:{Port} with ClientId: {ClientId}", 
            _config.HostName, _config.Port, _config.ClientId);

        // Simulate connection - in real implementation would use MQTTnet
        await Task.Delay(1000, cancellationToken);
        
        _logger.LogInformation("Successfully connected to MQTT broker");
    }

    private async Task SubscribeToTopicsAsync(CancellationToken cancellationToken)
    {
        var topics = _config.TopicFilters.Select(tf => tf.Topic).ToArray();

        _logger.LogInformation("Subscribing to topics: {Topics}", string.Join(", ", topics));

        foreach (var topicFilter in _config.TopicFilters)
        {
            _logger.LogDebug("Subscribing to topic {Topic} with QoS {QoS}", 
                topicFilter.Topic, topicFilter.QualityOfServiceLevel);
        }

        // Simulate subscription - in real implementation would use MQTTnet
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation("Successfully subscribed to {TopicCount} topics", topics.Length);
    }

    private async Task SimulateMessageReceptionAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var sampleData = new[]
        {
            ("messages/orders", "order", "{\"orderId\": \"12345\", \"productId\": \"widget-001\", \"quantity\": 5}"),
            ("messages/inventory", "inventory", "{\"productId\": \"widget-001\", \"stockLevel\": 100, \"location\": \"warehouse-a\"}"),
            ("messages/notifications", "notification", "{\"type\": \"email\", \"recipient\": \"user@example.com\", \"subject\": \"Order confirmed\"}"),
            ("messages/logs", "log", "{\"level\": \"info\", \"message\": \"System operation completed\", \"service\": \"order-processor\"}")
        };

        _logger.LogInformation("Starting message simulation...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Simulate receiving a message every 3-7 seconds
                var delay = random.Next(3000, 7000);
                await Task.Delay(delay, cancellationToken);

                var (topic, messageType, content) = sampleData[random.Next(sampleData.Length)];

                var message = new MqttMessage(content, topic, QosLevel.AtMostOnce, MessageType.Event, "mqtt-simulator");

                await ProcessMessageAsync(message, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simulated message");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<MqttMessageHandler>();

        _logger.LogDebug("Received MQTT message {MessageId} on topic {Topic}", message.Id, message.Topic);

        await handler.HandleAsync(message, cancellationToken);
    }

    private async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("Disconnecting from MQTT broker...");
            
            // Simulate disconnection - in real implementation would use MQTTnet
            await Task.Delay(500);
            
            _logger.LogInformation("Disconnected from MQTT broker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MQTT disconnection");
        }
    }
}
