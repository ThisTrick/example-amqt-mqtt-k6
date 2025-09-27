using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Shared.Models;

public class AmqpMessage : Message
{
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public byte Priority { get; set; } = 0;
    public bool Persistent { get; set; } = false;
    public int? Expiration { get; set; }

    public AmqpMessage() : base() { }

    public AmqpMessage(string payload, string exchange, string routingKey, MessageType messageType = MessageType.Event, string source = "")
        : base(payload, messageType, source)
    {
        Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        RoutingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
        Routing = $"{exchange}::{routingKey}";
    }

    public override bool IsValid()
    {
        return base.IsValid() && 
               !string.IsNullOrEmpty(Exchange) && 
               !string.IsNullOrEmpty(RoutingKey) &&
               Priority <= 255;
    }
}
