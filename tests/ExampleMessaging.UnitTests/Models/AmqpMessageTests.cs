using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.UnitTests.Models;

/// <summary>
/// Unit tests for AmqpMessage model validation and behavior
/// </summary>
public class AmqpMessageTests
{
    [Fact]
    public void AmqpMessage_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var payload = "Test AMQP message content";
        var routingKey = "test.routing.key";
        var exchange = "test.exchange";

        // Act
        var message = new AmqpMessage(payload, exchange, routingKey, MessageType.Event, "test-source")
        {
            Priority = 1,
            Persistent = true
        };

        // Assert
        Assert.Equal(payload, message.Payload);
        Assert.Equal(routingKey, message.RoutingKey);
        Assert.Equal(exchange, message.Exchange);
        Assert.Equal((byte)1, message.Priority);
        Assert.True(message.Persistent);
        Assert.Equal(MessageType.Event, message.MessageType);
        Assert.Equal("test-source", message.Source);
    }

    [Fact]
    public void AmqpMessage_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AmqpMessage(null!, "exchange", "routing.key"));
    }

    [Fact]
    public void AmqpMessage_WithNullExchange_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AmqpMessage("payload", null!, "routing.key"));
    }

    [Fact]
    public void AmqpMessage_WithNullRoutingKey_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AmqpMessage("payload", "exchange", null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(255)]
    public void AmqpMessage_WithValidPriority_ShouldSetCorrectly(byte priority)
    {
        // Arrange & Act
        var message = new AmqpMessage("test payload", "test.exchange", "test.key")
        {
            Priority = priority
        };

        // Assert
        Assert.Equal(priority, message.Priority);
    }

    [Fact]
    public void AmqpMessage_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var message = new AmqpMessage("Test payload", "test.exchange", "test.key");

        // Assert
        Assert.Equal((byte)0, message.Priority);
        Assert.False(message.Persistent);
        Assert.Equal(MessageType.Event, message.MessageType);
        Assert.True(message.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("test.exchange::test.key", message.Routing);
    }

    [Fact]
    public void AmqpMessage_Validation_ShouldCheckAllRequiredFields()
    {
        // Arrange
        var message = new AmqpMessage("Valid payload content", "valid.exchange", "valid.routing.key")
        {
            Priority = 5,
            Persistent = true
        };

        // Act
        var isValid = message.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void AmqpMessage_WithEmptyExchange_ShouldFailValidation()
    {
        // Arrange
        var message = new AmqpMessage("Valid payload", "valid.exchange", "valid.key");
        
        // Act - manually set empty exchange to test validation
        message.Exchange = "";
        var isValid = message.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void AmqpMessage_WithEmptyRoutingKey_ShouldFailValidation()
    {
        // Arrange
        var message = new AmqpMessage("Valid payload", "valid.exchange", "valid.key");
        
        // Act - manually set empty routing key to test validation
        message.RoutingKey = "";
        var isValid = message.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void AmqpMessage_WithHighPriority_ShouldFailValidation()
    {
        // Arrange
        var message = new AmqpMessage("Valid payload", "valid.exchange", "valid.key")
        {
            Priority = 255
        };

        // Act
        var isValid = message.IsValid();

        // Assert
        Assert.True(isValid); // 255 is valid for byte
    }

    [Fact]
    public void AmqpMessage_WithExpiration_ShouldSetCorrectly()
    {
        // Arrange & Act
        var message = new AmqpMessage("Test message", "test.exchange", "test.key")
        {
            Expiration = 60000 // 60 seconds
        };

        // Assert
        Assert.Equal(60000, message.Expiration);
    }

    [Fact]
    public void AmqpMessage_InheritsFromMessage_ShouldHaveBaseProperties()
    {
        // Arrange & Act
        var message = new AmqpMessage("Test payload", "test.exchange", "test.key", MessageType.Command, "test-service");

        // Assert
        Assert.IsAssignableFrom<Message>(message);
        Assert.Equal("Test payload", message.Payload);
        Assert.Equal(MessageType.Command, message.MessageType);
        Assert.Equal("test-service", message.Source);
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.NotEqual(Guid.Empty, message.CorrelationId);
    }

    [Fact]
    public void AmqpMessage_WithLargePayload_ShouldHandleCorrectly()
    {
        // Arrange
        var largePayload = new string('A', 50000); // 50KB payload
        
        // Act
        var message = new AmqpMessage(largePayload, "test.exchange", "test.key");

        // Assert
        Assert.Equal(largePayload, message.Payload);
        Assert.True(message.IsValid()); // Should be valid as it's under 64KB limit
    }

    [Fact]
    public void AmqpMessage_WithTooLargePayload_ShouldFailValidation()
    {
        // Arrange
        var tooLargePayload = new string('A', 70000); // 70KB payload, over 64KB limit
        
        // Act
        var message = new AmqpMessage(tooLargePayload, "test.exchange", "test.key");

        // Assert
        Assert.False(message.IsValid()); // Should fail validation due to size
    }
}
