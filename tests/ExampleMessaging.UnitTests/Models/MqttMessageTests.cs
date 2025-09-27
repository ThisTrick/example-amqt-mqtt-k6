using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.UnitTests.Models;

/// <summary>
/// Unit tests for MqttMessage model validation and behavior
/// </summary>
public class MqttMessageTests
{
    [Fact]
    public void MqttMessage_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var payload = "Test MQTT message content";
        var topic = "test/mqtt/topic";
        var qosLevel = QosLevel.AtLeastOnce;

        // Act
        var message = new MqttMessage(payload, topic, qosLevel, MessageType.Event, "test-source")
        {
            Retain = true
        };

        // Assert
        Assert.Equal(payload, message.Payload);
        Assert.Equal(topic, message.Topic);
        Assert.Equal(qosLevel, message.QosLevel);
        Assert.True(message.Retain);
        Assert.Equal(MessageType.Event, message.MessageType);
        Assert.Equal("test-source", message.Source);
        Assert.Equal(topic, message.Routing);
    }

    [Fact]
    public void MqttMessage_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MqttMessage(null!, "test/topic"));
    }

    [Fact] 
    public void MqttMessage_WithNullTopic_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MqttMessage("payload", null!));
    }

    [Theory]
    [InlineData("test/topic/with/slashes")]
    [InlineData("simple")]
    [InlineData("topic-with-dashes")]
    [InlineData("topic_with_underscores")]
    [InlineData("topic123")]
    [InlineData("$SYS/broker/uptime")]
    public void MqttMessage_WithValidTopic_ShouldSetCorrectly(string topic)
    {
        // Arrange & Act
        var message = new MqttMessage("Test payload", topic, QosLevel.AtMostOnce);

        // Assert
        Assert.Equal(topic, message.Topic);
        Assert.True(message.IsValid());
    }

    [Theory]
    [InlineData("topic/with/+/wildcard")]
    [InlineData("topic/with/#")]
    [InlineData("+/wildcard/topic")]
    [InlineData("#")]
    public void MqttMessage_WithWildcardTopic_ShouldFailValidation(string topic)
    {
        // Arrange & Act
        var message = new MqttMessage("Test payload", "valid/topic")
        {
            Topic = topic // Set invalid topic after construction
        };

        // Assert
        Assert.False(message.IsValid());
    }

    [Theory]
    [InlineData(QosLevel.AtMostOnce)]
    [InlineData(QosLevel.AtLeastOnce)]
    [InlineData(QosLevel.ExactlyOnce)]
    public void MqttMessage_WithValidQosLevel_ShouldSetCorrectly(QosLevel qosLevel)
    {
        // Arrange & Act
        var message = new MqttMessage("Test payload", "test/topic", qosLevel);

        // Assert
        Assert.Equal(qosLevel, message.QosLevel);
        Assert.True(message.IsValid());
    }

    [Fact]
    public void MqttMessage_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var message = new MqttMessage("Test payload", "test/topic");

        // Assert
        Assert.False(message.Retain);
        Assert.False(message.DupFlag);
        Assert.Equal(QosLevel.AtMostOnce, message.QosLevel);
        Assert.Equal(MessageType.Event, message.MessageType);
        Assert.True(message.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void MqttMessage_Validation_ShouldCheckAllRequiredFields()
    {
        // Arrange
        var message = new MqttMessage("Valid sensor reading: 23.5°C", "sensors/temperature/room1", QosLevel.AtLeastOnce)
        {
            Retain = false
        };

        // Act
        var isValid = message.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void MqttMessage_WithEmptyTopic_ShouldFailValidation()
    {
        // Arrange
        var message = new MqttMessage("Valid payload", "valid/topic")
        {
            Topic = "" // Set empty topic after construction
        };

        // Act
        var isValid = message.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void MqttMessage_WithRetainFlag_ShouldWorkWithAllQosLevels()
    {
        // Arrange & Act
        var qos0Message = new MqttMessage("Retained QoS 0", "test/retained/qos0", QosLevel.AtMostOnce)
        {
            Retain = true
        };

        var qos1Message = new MqttMessage("Retained QoS 1", "test/retained/qos1", QosLevel.AtLeastOnce)
        {
            Retain = true
        };

        var qos2Message = new MqttMessage("Retained QoS 2", "test/retained/qos2", QosLevel.ExactlyOnce)
        {
            Retain = true
        };

        // Assert
        Assert.True(qos0Message.Retain);
        Assert.True(qos1Message.Retain);
        Assert.True(qos2Message.Retain);
        Assert.All(new[] { qos0Message, qos1Message, qos2Message }, msg => Assert.True(msg.IsValid()));
    }

    [Fact]
    public void MqttMessage_InheritsFromMessage_ShouldHaveBaseProperties()
    {
        // Arrange & Act
        var message = new MqttMessage("Test payload", "test/topic", QosLevel.AtLeastOnce, MessageType.Command, "mqtt-service");

        // Assert
        Assert.IsAssignableFrom<Message>(message);
        Assert.Equal("Test payload", message.Payload);
        Assert.Equal(MessageType.Command, message.MessageType);
        Assert.Equal("mqtt-service", message.Source);
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.NotEqual(Guid.Empty, message.CorrelationId);
    }

    [Fact]
    public void MqttMessage_WithLongPayload_ShouldHandleCorrectly()
    {
        // Arrange
        var longPayload = new string('A', 10000); // 10KB payload
        
        // Act
        var message = new MqttMessage(longPayload, "test/large/message", QosLevel.AtLeastOnce);

        // Assert
        Assert.Equal(longPayload, message.Payload);
        Assert.Equal(10000, message.Payload.Length);
        Assert.True(message.IsValid());
    }

    [Fact]
    public void MqttMessage_WithTooLargePayload_ShouldFailValidation()
    {
        // Arrange
        var tooLargePayload = new string('A', 70000); // 70KB payload, over 64KB limit
        
        // Act
        var message = new MqttMessage(tooLargePayload, "test/large/message");

        // Assert
        Assert.False(message.IsValid()); // Should fail validation due to size
    }

    [Fact]
    public void MqttMessage_WithDupFlag_ShouldSetCorrectly()
    {
        // Arrange & Act
        var message = new MqttMessage("Duplicate message", "test/dup", QosLevel.AtLeastOnce)
        {
            DupFlag = true
        };

        // Assert
        Assert.True(message.DupFlag);
    }

    [Theory]
    [InlineData("home/living-room/temperature")]
    [InlineData("device/sensor-001/status")]
    [InlineData("metrics/performance/cpu")]
    [InlineData("alerts/critical/temperature")]
    public void MqttMessage_WithSystemTopics_ShouldValidateCorrectly(string systemTopic)
    {
        // Arrange & Act
        var message = new MqttMessage("System message", systemTopic, QosLevel.AtMostOnce);

        // Assert
        Assert.Equal(systemTopic, message.Topic);
        Assert.True(message.IsValid());
    }

    [Fact]
    public void MqttMessage_WithEmptyPayload_ShouldFailBaseValidation()
    {
        // Arrange
        var message = new MqttMessage("Valid payload", "test/topic")
        {
            Payload = "" // Set empty payload after construction
        };

        // Act
        var isValid = message.IsValid();

        // Assert
        Assert.False(isValid); // Should fail base Message validation
    }
}
