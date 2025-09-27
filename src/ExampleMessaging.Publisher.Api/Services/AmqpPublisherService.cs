using ExampleMessaging.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ExampleMessaging.Publisher.Api.Services;

/// <summary>
/// Service for publishing messages to AMQP (RabbitMQ) broker
/// </summary>
public interface IAmqpPublisherService
{
    Task PublishAsync(AmqpMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public class AmqpPublisherService : IAmqpPublisherService, IDisposable
{
    private readonly AmqpConfig _config;
    private readonly ILogger<AmqpPublisherService> _logger;
    private bool _disposed = false;

    public AmqpPublisherService(IOptions<AmqpConfig> config, ILogger<AmqpPublisherService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("AMQP publisher configured for {HostName}:{Port}", _config.HostName, _config.Port);
    }

    public async Task PublishAsync(AmqpMessage message, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AmqpPublisherService));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!message.IsValid())
            throw new ArgumentException("Invalid message", nameof(message));

        // Simulate publishing for now - in a real implementation, this would connect to RabbitMQ
        _logger.LogInformation("Publishing AMQP message {Id} to {Exchange}/{RoutingKey}", 
            message.Id, message.Exchange, message.RoutingKey);

        var jsonPayload = JsonSerializer.Serialize(message);
        _logger.LogDebug("Message payload: {Payload}", jsonPayload);

        // Simulate async operation
        await Task.Delay(10, cancellationToken);
        
        _logger.LogDebug("Published AMQP message {Id} successfully", message.Id);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        // Simulate health check
        await Task.Delay(5, cancellationToken);
        
        _logger.LogDebug("AMQP service health check passed");
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing AMQP publisher service");
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Configuration for AMQP connection
/// </summary>
public class AmqpConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "messaging.exchange";
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public bool TopologyRecoveryEnabled { get; set; } = true;
}
