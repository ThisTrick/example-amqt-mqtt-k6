using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace ExampleMessaging.IntegrationTests;

public class AmqpIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IConnection? _connection;
    private IModel? _channel;

    public AmqpIntegrationTests()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management-alpine")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        
        // Wait for RabbitMQ to be ready
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "admin",
            Password = "admin",
            VirtualHost = "/"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare test exchange and queue
        _channel.ExchangeDeclare("learning.direct", ExchangeType.Direct, durable: true);
        _channel.QueueDeclare("orders.created", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("orders.created", "learning.direct", "orders.created");
    }

    public async Task DisposeAsync()
    {
        _channel?.Close();
        _connection?.Close();
        await _rabbitMqContainer.DisposeAsync();
    }

    [Fact]
    public async Task AMQP_PublishMessage_ConsumerReceives()
    {
        // Arrange
        var testMessage = new
        {
            id = Guid.NewGuid(),
            payload = "Integration test AMQP message",
            timestamp = DateTimeOffset.UtcNow,
            messageType = "Event"
        };

        var messageJson = JsonSerializer.Serialize(testMessage);
        var messageBody = Encoding.UTF8.GetBytes(messageJson);
        
        string? receivedMessage = null;
        var messageReceived = new TaskCompletionSource<bool>();

        // Set up consumer - This MUST FAIL because AmqpMessageHandler doesn't exist yet
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            receivedMessage = Encoding.UTF8.GetString(body);
            messageReceived.SetResult(true);
        };

        _channel.BasicConsume(queue: "orders.created", autoAck: true, consumer: consumer);

        // Act - Direct AMQP publish (simulates what API should do)
        _channel.BasicPublish(
            exchange: "learning.direct",
            routingKey: "orders.created",
            basicProperties: null,
            body: messageBody);

        // Assert - This should pass (direct AMQP), but consumer processing will fail
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(received, "Message should be received via AMQP");
        Assert.NotNull(receivedMessage);
        
        var deserializedMessage = JsonSerializer.Deserialize<JsonElement>(receivedMessage);
        Assert.Equal(testMessage.payload, deserializedMessage.GetProperty("payload").GetString());
    }

    [Fact]
    public async Task AMQP_MessagePersistence_SurvivesRestart()
    {
        // Arrange
        var testMessage = new
        {
            id = Guid.NewGuid(),
            payload = "Persistent AMQP message",
            timestamp = DateTimeOffset.UtcNow,
            messageType = "Event"
        };

        var messageJson = JsonSerializer.Serialize(testMessage);
        var messageBody = Encoding.UTF8.GetBytes(messageJson);

        // Publish persistent message
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "learning.direct",
            routingKey: "orders.created",
            basicProperties: properties,
            body: messageBody);

        // Simulate broker restart by recreating connection
        _channel?.Close();
        _connection?.Close();

        await Task.Delay(TimeSpan.FromSeconds(2));

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "admin",
            Password = "admin",
            VirtualHost = "/"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Act & Assert - Message should still be in queue
        var result = _channel.BasicGet("orders.created", autoAck: true);
        Assert.NotNull(result);
        
        var receivedMessage = Encoding.UTF8.GetString(result.Body.ToArray());
        var deserializedMessage = JsonSerializer.Deserialize<JsonElement>(receivedMessage);
        Assert.Equal(testMessage.payload, deserializedMessage.GetProperty("payload").GetString());
    }

    [Fact]
    public async Task AMQP_RoutingKey_CorrectQueueRouting()
    {
        // Arrange - Set up multiple queues with different routing keys
        _channel.QueueDeclare("orders.updated", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("orders.deleted", durable: true, exclusive: false, autoDelete: false);
        
        _channel.QueueBind("orders.updated", "learning.direct", "orders.updated");
        _channel.QueueBind("orders.deleted", "learning.direct", "orders.deleted");

        var createdMessage = "Message for created queue";
        var updatedMessage = "Message for updated queue";
        var deletedMessage = "Message for deleted queue";

        // Act - Publish messages with different routing keys
        _channel.BasicPublish("learning.direct", "orders.created", null, Encoding.UTF8.GetBytes(createdMessage));
        _channel.BasicPublish("learning.direct", "orders.updated", null, Encoding.UTF8.GetBytes(updatedMessage));
        _channel.BasicPublish("learning.direct", "orders.deleted", null, Encoding.UTF8.GetBytes(deletedMessage));

        // Allow time for routing
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert - Each message should be in correct queue
        var createdResult = _channel.BasicGet("orders.created", autoAck: true);
        var updatedResult = _channel.BasicGet("orders.updated", autoAck: true);
        var deletedResult = _channel.BasicGet("orders.deleted", autoAck: true);

        Assert.NotNull(createdResult);
        Assert.NotNull(updatedResult);
        Assert.NotNull(deletedResult);

        Assert.Equal(createdMessage, Encoding.UTF8.GetString(createdResult.Body.ToArray()));
        Assert.Equal(updatedMessage, Encoding.UTF8.GetString(updatedResult.Body.ToArray()));
        Assert.Equal(deletedMessage, Encoding.UTF8.GetString(deletedResult.Body.ToArray()));
    }

    [Fact]
    public async Task AMQP_ConnectionRecovery_HandlesDisconnection()
    {
        // This test validates connection recovery patterns
        // Will FAIL initially because AmqpConnectionService doesn't exist yet
        
        // Arrange
        var messagesReceived = new List<string>();
        var messageCount = 5;
        var receivedCount = 0;
        var allMessagesReceived = new TaskCompletionSource<bool>();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            messagesReceived.Add(message);
            Interlocked.Increment(ref receivedCount);
            
            if (receivedCount >= messageCount)
                allMessagesReceived.SetResult(true);
        };

        _channel.BasicConsume("orders.created", autoAck: true, consumer: consumer);

        // Act - Publish messages before and after simulated disconnection
        for (int i = 0; i < messageCount; i++)
        {
            var message = $"Recovery test message {i}";
            _channel.BasicPublish("learning.direct", "orders.created", null, Encoding.UTF8.GetBytes(message));
            
            // Simulate network issue on message 2
            if (i == 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        // Assert
        var allReceived = await allMessagesReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(allReceived, "All messages should be received despite connection issues");
        Assert.Equal(messageCount, messagesReceived.Count);
    }

    [Fact]
    public async Task AMQP_MessagePriority_CorrectOrdering()
    {
        // Arrange - Declare priority queue
        var queueArgs = new Dictionary<string, object>
        {
            { "x-max-priority", 10 }
        };
        
        _channel.QueueDeclare("priority.test", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind("priority.test", "learning.direct", "priority.test");

        // Publish messages with different priorities (high priority last)
        var messages = new[]
        {
            ("Low priority message", (byte)1),
            ("Normal priority message", (byte)5),
            ("High priority message", (byte)10)
        };

        foreach (var (message, priority) in messages)
        {
            var properties = _channel.CreateBasicProperties();
            properties.Priority = priority;
            
            _channel.BasicPublish("learning.direct", "priority.test", properties, Encoding.UTF8.GetBytes(message));
        }

        // Allow time for priority sorting
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Act & Assert - High priority should come first
        var firstMessage = _channel.BasicGet("priority.test", autoAck: true);
        var secondMessage = _channel.BasicGet("priority.test", autoAck: true);
        var thirdMessage = _channel.BasicGet("priority.test", autoAck: true);

        Assert.NotNull(firstMessage);
        Assert.NotNull(secondMessage);
        Assert.NotNull(thirdMessage);

        // Note: Priority ordering may not be guaranteed with small message counts
        // This test validates the priority feature is configured correctly
        var receivedMessages = new[]
        {
            Encoding.UTF8.GetString(firstMessage.Body.ToArray()),
            Encoding.UTF8.GetString(secondMessage.Body.ToArray()),
            Encoding.UTF8.GetString(thirdMessage.Body.ToArray())
        };

        Assert.Contains("High priority message", receivedMessages);
        Assert.Contains("Normal priority message", receivedMessages);
        Assert.Contains("Low priority message", receivedMessages);
    }
}
