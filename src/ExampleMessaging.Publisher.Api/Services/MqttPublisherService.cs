using ExampleMessaging.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ExampleMessaging.Publisher.Api.Services;

/// <summary>
/// Service for publishing messages to MQTT broker
/// </summary>
public interface IMqttPublisherService
{
    Task PublishAsync(MqttMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public class MqttPublisherService : IMqttPublisherService, IDisposable
{
    private readonly MqttConfig _config;
    private readonly ILogger<MqttPublisherService> _logger;
    private bool _disposed = false;

    public MqttPublisherService(IOptions<MqttConfig> config, ILogger<MqttPublisherService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("MQTT publisher configured for {Server}:{Port}", _config.Server, _config.Port);
    }

    public async Task PublishAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MqttPublisherService));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!message.IsValid())
            throw new ArgumentException("Invalid message", nameof(message));

        // Simulate publishing for now - in a real implementation, this would connect to MQTT broker
        _logger.LogInformation("Publishing MQTT message {Id} to topic {Topic}", 
            message.Id, message.Topic);

        var jsonPayload = JsonSerializer.Serialize(message);
        _logger.LogDebug("Message payload: {Payload}", jsonPayload);

        // Simulate async operation
        await Task.Delay(10, cancellationToken);
        
        _logger.LogDebug("Published MQTT message {Id} successfully", message.Id);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        // Simulate health check
        await Task.Delay(5, cancellationToken);
        
        _logger.LogDebug("MQTT service health check passed");
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing MQTT publisher service");
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Configuration for MQTT connection
/// </summary>
public class MqttConfig
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "ExampleMessaging.Publisher";
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public int KeepAlivePeriodSeconds { get; set; } = 60;
    public bool CleanSession { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 30;
}
