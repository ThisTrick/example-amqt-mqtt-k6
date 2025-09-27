using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Shared.Configuration;

public class MessagingConfiguration
{
    public const string SectionName = "Messaging";

    public AmqpConfiguration Amqp { get; set; } = new();
    public MqttConfiguration Mqtt { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
}

public class AmqpConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "learning.direct";
    public bool AutoReconnect { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 5;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class MqttConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string ClientId { get; set; } = $"ExampleMessaging-{Environment.MachineName}";
    public bool CleanSession { get; set; } = true;
    public QosLevel DefaultQosLevel { get; set; } = QosLevel.AtLeastOnce;
    public bool AutoReconnect { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 5;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class LoggingConfiguration
{
    public string Level { get; set; } = "Information";
    public bool StructuredLogging { get; set; } = true;
    public bool EnableCorrelationId { get; set; } = true;
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
}
