namespace ExampleMessaging.Shared.Configuration;

public class MqttConsumerConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "mqtt-consumer";
    public List<MqttTopicFilter> TopicFilters { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 30;
    public int KeepAliveSeconds { get; set; } = 60;
    public bool CleanSession { get; set; } = true;
    public int RetryIntervalSeconds { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
}

public class MqttTopicFilter
{
    public string Topic { get; set; } = string.Empty;
    public int QualityOfServiceLevel { get; set; } = 0;
}
