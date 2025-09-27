namespace ExampleMessaging.Shared.Models;

public enum MessageType
{
    Event,
    Command,
    Query
}

public enum QosLevel
{
    AtMostOnce = 0,
    AtLeastOnce = 1,
    ExactlyOnce = 2
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

public enum ProtocolType
{
    Amqp,
    Mqtt
}
