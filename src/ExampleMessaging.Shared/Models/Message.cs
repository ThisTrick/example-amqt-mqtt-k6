namespace ExampleMessaging.Shared.Models;

public abstract class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public MessageType MessageType { get; set; } = MessageType.Event;
    public string Source { get; set; } = string.Empty;
    public string Routing { get; set; } = string.Empty;

    protected Message() { }

    protected Message(string payload, MessageType messageType, string source)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        MessageType = messageType;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public virtual bool IsValid()
    {
        return !string.IsNullOrEmpty(Payload) && 
               Payload.Length <= 65536 && // 64KB limit
               Timestamp != default &&
               Id != Guid.Empty;
    }
}
