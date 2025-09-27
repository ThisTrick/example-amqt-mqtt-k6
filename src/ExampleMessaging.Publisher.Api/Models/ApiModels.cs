using System.ComponentModel.DataAnnotations;

namespace ExampleMessaging.Publisher.Api.Models;

public class AmqpPublishRequest
{
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    [Required(ErrorMessage = "Exchange is required")]
    public string Exchange { get; set; } = string.Empty;

    [Required(ErrorMessage = "RoutingKey is required")]
    public string RoutingKey { get; set; } = string.Empty;

    public string MessageType { get; set; } = "Event";
    public bool Persistent { get; set; } = false;
    public byte Priority { get; set; } = 0;
    public int? Expiration { get; set; }
}

public class MqttPublishRequest
{
    [Required(ErrorMessage = "Payload is required")]
    public string Payload { get; set; } = string.Empty;

    [Required(ErrorMessage = "Topic is required")]
    public string Topic { get; set; } = string.Empty;

    [Range(0, 2, ErrorMessage = "QosLevel must be 0, 1, or 2")]
    public int QosLevel { get; set; } = 1;

    public bool Retain { get; set; } = false;
    public string MessageType { get; set; } = "Event";
}

public class PublishResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = "accepted";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Retained { get; set; } = false;
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, DependencyHealth> Dependencies { get; set; } = new();
}

public class DependencyHealth
{
    public string Status { get; set; } = string.Empty;
    public double ResponseTime { get; set; }
}

public class MetricsResponse
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AmqpMetrics Amqp { get; set; } = new();
    public MqttMetrics Mqtt { get; set; } = new();
    public SystemMetrics System { get; set; } = new();
}

public class AmqpMetrics
{
    public long MessagesPublished { get; set; }
    public long MessagesConsumed { get; set; }
    public int ConnectionCount { get; set; }
    public double AverageLatency { get; set; }
    public double PublishRate { get; set; }
    public double ConsumeRate { get; set; }
    public long PublishErrors { get; set; }
    public long ConsumeErrors { get; set; }
}

public class MqttMetrics
{
    public long MessagesPublished { get; set; }
    public long MessagesConsumed { get; set; }
    public int ConnectionCount { get; set; }
    public double AverageLatency { get; set; }
    public double PublishRate { get; set; }
    public double ConsumeRate { get; set; }
    public long PublishErrors { get; set; }
    public long ConsumeErrors { get; set; }
}

public class SystemMetrics
{
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public long Uptime { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
