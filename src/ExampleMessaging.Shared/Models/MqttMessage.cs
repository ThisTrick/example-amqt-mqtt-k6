using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Shared.Models;

public class MqttMessage : Message
{
    public string Topic { get; set; } = string.Empty;
    public QosLevel QosLevel { get; set; } = QosLevel.AtMostOnce;
    public bool Retain { get; set; } = false;
    public bool DupFlag { get; set; } = false;

    public MqttMessage() : base() { }

    public MqttMessage(string payload, string topic, QosLevel qosLevel = QosLevel.AtMostOnce, MessageType messageType = MessageType.Event, string source = "")
        : base(payload, messageType, source)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        QosLevel = qosLevel;
        Routing = topic;
    }

    public override bool IsValid()
    {
        return base.IsValid() && 
               !string.IsNullOrEmpty(Topic) && 
               IsValidTopic(Topic) &&
               Enum.IsDefined(typeof(QosLevel), QosLevel);
    }

    private static bool IsValidTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            return false;

        // MQTT topic validation rules
        // - Cannot contain wildcards in publish topics
        // - Must not contain null characters
        // - Should not start or end with /
        if (topic.Contains('#') || topic.Contains('+'))
            return false;

        if (topic.Contains('\0'))
            return false;

        return true;
    }
}
