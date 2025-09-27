using ExampleMessaging.Shared.Configuration;
using ExampleMessaging.Shared.Models;
using ExampleMessaging.Amqp.Consumer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ExampleMessaging.Amqp.Consumer.Services;

/// <summary>
/// Service for managing AMQP connection and consuming messages
/// </summary>
public interface IAmqpConnectionService
{
    Task<bool> IsHealthyAsync();
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    Task StopConsumingAsync();
}

public class AmqpConnectionService : BackgroundService, IAmqpConnectionService
{
    private readonly AmqpConsumerConfig _config;
    private readonly ILogger<AmqpConnectionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    private bool _isConsuming = false;
    private bool _isHealthy = false;

    public AmqpConnectionService(
        IOptions<AmqpConsumerConfig> config, 
        ILogger<AmqpConnectionService> logger,
        IServiceProvider serviceProvider)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartConsumingAsync(stoppingToken);
        
        // Keep the service running and simulate message consumption
        while (!stoppingToken.IsCancellationRequested && _isConsuming)
        {
            try
            {
                // Simulate receiving messages periodically
                await SimulateMessageReception(stoppingToken);
                await Task.Delay(5000, stoppingToken); // Check every 5 seconds
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AMQP consumer monitoring loop");
                _isHealthy = false;
                await Task.Delay(10000, stoppingToken); // Wait before retry
            }
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        return _isHealthy && _isConsuming;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting AMQP consumer for broker at {HostName}:{Port}", 
                _config.HostName, _config.Port);

            // Simulate connection setup
            await Task.Delay(1000, cancellationToken);

            _logger.LogInformation("Connected to AMQP broker and configured queue {Queue} with routing keys: {RoutingKeys}", 
                _config.QueueName, string.Join(", ", _config.RoutingKeys));

            _isConsuming = true;
            _isHealthy = true;
            
            _logger.LogInformation("AMQP consumer started successfully and listening for messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AMQP consumer");
            _isHealthy = false;
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            _logger.LogInformation("Stopping AMQP consumer...");
            
            _isConsuming = false;
            _isHealthy = false;
            
            // Simulate cleanup
            await Task.Delay(500);
            
            _logger.LogInformation("AMQP consumer stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping AMQP consumer");
        }
    }

    private async Task SimulateMessageReception(CancellationToken cancellationToken)
    {
        // Simulate periodic message reception
        var random = new Random();
        
        // Randomly decide if we should simulate receiving a message (30% chance every check)
        if (random.NextDouble() < 0.3)
        {
            await ProcessSimulatedMessage();
        }
    }

    private async Task ProcessSimulatedMessage()
    {
        try
        {
            // Create a simulated message
            var simulatedMessage = CreateSimulatedMessage();
            var routingKey = GetRandomRoutingKey();

            _logger.LogDebug("Simulating reception of AMQP message {MessageId} with routing key {RoutingKey}", 
                simulatedMessage.Id, routingKey);

            // Process message using handler
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IAmqpMessageHandler>();
            
            var success = await handler.HandleAsync(simulatedMessage, routingKey);
            
            if (success)
            {
                _logger.LogDebug("Successfully processed simulated message {MessageId}", simulatedMessage.Id);
            }
            else
            {
                _logger.LogWarning("Failed to process simulated message {MessageId}", simulatedMessage.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing simulated AMQP message");
        }
    }

    private AmqpMessage CreateSimulatedMessage()
    {
        var random = new Random();
        var messageTypes = new[] { "order", "user", "product", "payment", "notification" };
        var messageType = messageTypes[random.Next(messageTypes.Length)];
        
        var message = new AmqpMessage(
            payload: JsonSerializer.Serialize(new { 
                type = messageType, 
                data = $"Simulated {messageType} data", 
                timestamp = DateTimeOffset.UtcNow 
            }),
            exchange: _config.Exchange,
            routingKey: $"message.{messageType}"
        );
        
        message.Priority = (byte)random.Next(0, 256);
        message.MessageType = random.NextDouble() < 0.7 ? MessageType.Event : MessageType.Command;
        
        return message;
    }

    private string GetRandomRoutingKey()
    {
        var random = new Random();
        return _config.RoutingKeys[random.Next(_config.RoutingKeys.Count)];
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
}
