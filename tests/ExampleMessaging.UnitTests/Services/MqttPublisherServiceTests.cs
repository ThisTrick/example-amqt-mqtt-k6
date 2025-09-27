using ExampleMessaging.Publisher.Api.Services;
using ExampleMessaging.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExampleMessaging.UnitTests.Services;

/// <summary>
/// Unit tests for MQTT publisher service
/// </summary>
public class MqttPublisherServiceTests
{
    [Fact]
    public void MqttConfig_WithDefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new MqttConfig();

        // Assert
        Assert.Equal("localhost", config.Server);
        Assert.Equal(1883, config.Port);
        Assert.Equal("ExampleMessaging.Publisher", config.ClientId);
        Assert.Equal(60, config.KeepAlivePeriodSeconds);
        Assert.True(config.CleanSession);
        Assert.Equal(30, config.ConnectTimeoutSeconds);
    }

    [Fact]
    public void MqttPublisherService_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MqttPublisherService(null!, mockLogger.Object));
    }

    [Fact]
    public void MqttPublisherService_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new MqttPublisherService(mockOptions.Object, null!));
    }

    [Fact]
    public async Task MqttPublisherService_PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttPublisherService(mockOptions.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.PublishAsync(null!));
    }

    [Fact]
    public async Task MqttPublisherService_PublishAsync_WithInvalidMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttPublisherService(mockOptions.Object, mockLogger.Object);

        var invalidMessage = new MqttMessage("valid payload", "valid/topic")
        {
            Topic = "" // Make it invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.PublishAsync(invalidMessage));
    }

    [Fact]
    public async Task MqttPublisherService_PublishAsync_WithValidMessage_ShouldCompleteSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttPublisherService(mockOptions.Object, mockLogger.Object);

        var validMessage = new MqttMessage("test payload", "test/topic");

        // Act & Assert
        await service.PublishAsync(validMessage);
        
        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task MqttPublisherService_IsHealthyAsync_ShouldReturnTrue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttPublisherService(mockOptions.Object, mockLogger.Object);

        // Act
        var result = await service.IsHealthyAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MqttPublisherService_Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MqttPublisherService>>();
        var config = new MqttConfig();
        var mockOptions = new Mock<IOptions<MqttConfig>>();
        mockOptions.Setup(x => x.Value).Returns(config);

        var service = new MqttPublisherService(mockOptions.Object, mockLogger.Object);

        // Act & Assert
        service.Dispose(); // Should not throw
        Assert.True(true);
    }
}
