using ExampleMessaging.Publisher.Api.Services;
using ExampleMessaging.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExampleMessaging.UnitTests.Services;

/// <summary>
/// Unit tests for AMQP publisher service
/// </summary>
public class AmqpPublisherServiceTests
{
    [Fact]
    public void AmqpConfig_WithDefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new AmqpConfig();

        // Assert
        Assert.Equal("localhost", config.HostName);
        Assert.Equal(5672, config.Port);
        Assert.Equal("guest", config.UserName);
        Assert.Equal("guest", config.Password);
        Assert.Equal("/", config.VirtualHost);
        Assert.Equal("messaging.exchange", config.Exchange);
        Assert.True(config.AutomaticRecoveryEnabled);
        Assert.True(config.TopologyRecoveryEnabled);
    }

    [Fact]
    public void AmqpPublisherService_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AmqpPublisherService(null!, mockLogger.Object));
    }

    [Fact]
    public void AmqpPublisherService_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AmqpPublisherService(mockOptions.Object, null!));
    }

    [Fact]
    public async Task AmqpPublisherService_PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new AmqpPublisherService(mockOptions.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.PublishAsync(null!));
    }

    [Fact]
    public async Task AmqpPublisherService_PublishAsync_WithInvalidMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new AmqpPublisherService(mockOptions.Object, mockLogger.Object);

        var invalidMessage = new AmqpMessage("valid payload", "valid exchange", "valid key")
        {
            Exchange = "" // Make it invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.PublishAsync(invalidMessage));
    }

    [Fact]
    public async Task AmqpPublisherService_PublishAsync_WithValidMessage_ShouldCompleteSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new AmqpPublisherService(mockOptions.Object, mockLogger.Object);

        var validMessage = new AmqpMessage("test payload", "test.exchange", "test.key");

        // Act & Assert
        await service.PublishAsync(validMessage);
        
        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task AmqpPublisherService_IsHealthyAsync_ShouldReturnTrue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new AmqpPublisherService(mockOptions.Object, mockLogger.Object);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AmqpPublisherService_Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AmqpPublisherService>>();
        var config = new AmqpConfig();
        var mockOptions = new Mock<IOptions<AmqpConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new AmqpPublisherService(mockOptions.Object, mockLogger.Object);

        // Act & Assert
        service.Dispose(); // Should not throw
        Assert.True(true);
    }
}
