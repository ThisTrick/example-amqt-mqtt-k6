namespace ExampleMessaging.Shared.Configuration;

public class AmqpConsumerConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "messaging.exchange";
    public string QueueName { get; set; } = "messaging.queue";
    public List<string> RoutingKeys { get; set; } = new();
    public ushort PrefetchCount { get; set; } = 10;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public bool TopologyRecoveryEnabled { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 30;
    public int HeartbeatSeconds { get; set; } = 60;
}
