using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.UnitTests.Models;

/// <summary>
/// Unit tests for ConnectionInfo model state transitions and behavior
/// </summary>
public class ConnectionInfoTests
{
    [Fact]
    public void ConnectionInfo_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var hostName = "localhost";
        var port = 5672;
        var protocol = ProtocolType.Amqp;

        // Act
        var connectionInfo = new ConnectionInfo(protocol, hostName, port, "testuser", "testpass");

        // Assert
        Assert.Equal(hostName, connectionInfo.Host);
        Assert.Equal(port, connectionInfo.Port);
        Assert.Equal(protocol, connectionInfo.Protocol);
        Assert.Equal("testuser", connectionInfo.Username);
        Assert.Equal("testpass", connectionInfo.Password);
        Assert.Equal(ConnectionStatus.Disconnected, connectionInfo.Status);
    }

    [Fact]
    public void ConnectionInfo_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Mqtt, "localhost", 1883);

        // Assert
        Assert.Equal(ConnectionStatus.Disconnected, connectionInfo.Status);
        Assert.Equal("/", connectionInfo.VirtualHost);
        Assert.Equal(0, connectionInfo.RetryAttempts);
        Assert.Equal(5, connectionInfo.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), connectionInfo.ConnectionTimeout);
        Assert.Equal(string.Empty, connectionInfo.Username);
        Assert.Equal(string.Empty, connectionInfo.Password);
        Assert.NotEqual(Guid.Empty, connectionInfo.Id);
    }

    [Fact]
    public void ConnectionInfo_WithNullHost_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConnectionInfo(ProtocolType.Amqp, null!, 5672));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void ConnectionInfo_WithInvalidPort_ShouldFailValidation(int port)
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", port);

        // Assert
        Assert.False(connectionInfo.IsValid());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(1883)]
    [InlineData(5672)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void ConnectionInfo_WithValidPort_ShouldPassValidation(int port)
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", port);

        // Assert
        Assert.True(connectionInfo.IsValid());
        Assert.Equal(port, connectionInfo.Port);
    }

    [Fact]
    public void ConnectionInfo_UpdateStatus_ToConnected_ShouldSetTimestamps()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        connectionInfo.UpdateStatus(ConnectionStatus.Connected);

        // Assert
        Assert.Equal(ConnectionStatus.Connected, connectionInfo.Status);
        Assert.True(connectionInfo.LastConnected >= beforeUpdate);
        Assert.True(connectionInfo.LastHeartbeat >= beforeUpdate);
        Assert.Equal(0, connectionInfo.RetryAttempts);
    }

    [Fact]
    public void ConnectionInfo_UpdateStatus_ToFailed_ShouldIncrementRetryAttempts()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);
        var initialRetryAttempts = connectionInfo.RetryAttempts;

        // Act
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);

        // Assert
        Assert.Equal(ConnectionStatus.Failed, connectionInfo.Status);
        Assert.Equal(initialRetryAttempts + 1, connectionInfo.RetryAttempts);
    }

    [Fact]
    public void ConnectionInfo_UpdateStatus_ToDisconnected_ShouldIncrementRetryAttempts()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);
        connectionInfo.UpdateStatus(ConnectionStatus.Connected); // First connect
        var initialRetryAttempts = connectionInfo.RetryAttempts; // Should be 0 after connected

        // Act
        connectionInfo.UpdateStatus(ConnectionStatus.Disconnected);

        // Assert
        Assert.Equal(ConnectionStatus.Disconnected, connectionInfo.Status);
        Assert.Equal(initialRetryAttempts + 1, connectionInfo.RetryAttempts);
    }

    [Fact]
    public void ConnectionInfo_CanRetry_ShouldReturnTrueWhenUnderLimit()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);

        // Act & Assert
        Assert.True(connectionInfo.CanRetry()); // 0 < 5 (default max)

        // Simulate some retries
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);

        Assert.True(connectionInfo.CanRetry()); // 3 < 5
    }

    [Fact]
    public void ConnectionInfo_CanRetry_ShouldReturnFalseWhenOverLimit()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672)
        {
            MaxRetryAttempts = 3
        };

        // Act - Simulate reaching retry limit
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);
        connectionInfo.UpdateStatus(ConnectionStatus.Failed);

        // Assert
        Assert.False(connectionInfo.CanRetry()); // 3 >= 3
    }

    [Fact]
    public void ConnectionInfo_ResetHeartbeat_ShouldUpdateLastHeartbeat()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);
        var initialHeartbeat = connectionInfo.LastHeartbeat;

        // Wait a small amount to ensure time difference
        Thread.Sleep(10);

        // Act
        connectionInfo.ResetHeartbeat();

        // Assert
        Assert.True(connectionInfo.LastHeartbeat > initialHeartbeat);
    }

    [Fact]
    public void ConnectionInfo_IsHeartbeatExpired_ShouldReturnFalseForRecentHeartbeat()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672);
        connectionInfo.ResetHeartbeat(); // Set recent heartbeat

        // Act & Assert
        Assert.False(connectionInfo.IsHeartbeatExpired());
    }

    [Fact]
    public void ConnectionInfo_IsHeartbeatExpired_ShouldReturnTrueForOldHeartbeat()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672)
        {
            ConnectionTimeout = TimeSpan.FromMilliseconds(50) // Very short timeout
        };
        
        // Set an old heartbeat
        connectionInfo.ResetHeartbeat();
        
        // Wait longer than timeout
        Thread.Sleep(100);

        // Act & Assert
        Assert.True(connectionInfo.IsHeartbeatExpired());
    }

    [Fact]
    public void ConnectionInfo_IsValid_ShouldCheckAllRequiredFields()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Mqtt, "valid.broker.com", 1883, "user", "pass");

        // Act
        var isValid = connectionInfo.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ConnectionInfo_WithEmptyHost_ShouldFailValidation()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672)
        {
            Host = "" // Set empty host after construction
        };

        // Act
        var isValid = connectionInfo.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(ProtocolType.Amqp, 5672)]
    [InlineData(ProtocolType.Mqtt, 1883)]
    public void ConnectionInfo_WithProtocolDefaults_ShouldSetCorrectPorts(ProtocolType protocol, int expectedPort)
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(protocol, "localhost", expectedPort);

        // Assert
        Assert.Equal(protocol, connectionInfo.Protocol);
        Assert.Equal(expectedPort, connectionInfo.Port);
        Assert.True(connectionInfo.IsValid());
    }

    [Fact]
    public void ConnectionInfo_WithVirtualHost_ShouldSetCorrectly()
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672)
        {
            VirtualHost = "/production"
        };

        // Assert
        Assert.Equal("/production", connectionInfo.VirtualHost);
    }

    [Fact]
    public void ConnectionInfo_CompleteConnectionCycle_ShouldTrackAllStates()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo(ProtocolType.Mqtt, "test.broker.com", 1883);

        // Act & Assert - Complete connection cycle
        Assert.Equal(ConnectionStatus.Disconnected, connectionInfo.Status);
        Assert.Equal(0, connectionInfo.RetryAttempts);

        connectionInfo.UpdateStatus(ConnectionStatus.Connecting);
        Assert.Equal(ConnectionStatus.Connecting, connectionInfo.Status);

        connectionInfo.UpdateStatus(ConnectionStatus.Connected);
        Assert.Equal(ConnectionStatus.Connected, connectionInfo.Status);
        Assert.Equal(0, connectionInfo.RetryAttempts); // Reset on successful connection
        Assert.True(connectionInfo.LastConnected > DateTimeOffset.MinValue);

        connectionInfo.ResetHeartbeat();
        Assert.True(connectionInfo.LastHeartbeat > DateTimeOffset.MinValue);

        connectionInfo.UpdateStatus(ConnectionStatus.Disconnected);
        Assert.Equal(ConnectionStatus.Disconnected, connectionInfo.Status);
        Assert.Equal(1, connectionInfo.RetryAttempts); // Incremented on disconnect
    }

    [Fact]
    public void ConnectionInfo_WithCredentials_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Mqtt, "secure.broker.com", 8883, "secureuser", "securepassword");

        // Assert
        Assert.Equal("secureuser", connectionInfo.Username);
        Assert.Equal("securepassword", connectionInfo.Password);
        Assert.True(connectionInfo.IsValid());
    }

    [Fact]
    public void ConnectionInfo_CustomTimeout_ShouldSetCorrectly()
    {
        // Arrange & Act
        var connectionInfo = new ConnectionInfo(ProtocolType.Amqp, "localhost", 5672)
        {
            ConnectionTimeout = TimeSpan.FromMinutes(2)
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), connectionInfo.ConnectionTimeout);
    }
}
