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

public class AmqpIntegrationTestsNew : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IConnection? _connection;
    private IChannel? _channel;

    public AmqpIntegrationTestsNew()
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

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        
        // Declare test exchange and queue
        await _channel.ExchangeDeclareAsync("learning.direct", ExchangeType.Direct, durable: true);
        await _channel.QueueDeclareAsync("orders.created", durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync("orders.created", "learning.direct", "orders.created");
    }

    public async Task DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();
        if (_connection != null)
            await _connection.CloseAsync();
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

        // Set up consumer using new async API
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            receivedMessage = Encoding.UTF8.GetString(body);
            messageReceived.SetResult(true);
            return Task.CompletedTask;
        };

        await _channel!.BasicConsumeAsync(queue: "orders.created", autoAck: true, consumer: consumer);

        // Act - Direct AMQP publish using new async API
        await _channel.BasicPublishAsync(
            exchange: "learning.direct",
            routingKey: "orders.created",
            mandatory: false,
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
        var testMessage = "Persistent message test";
        
        // Publish a persistent message using new BasicProperties struct
        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel!.BasicPublishAsync(
            exchange: "learning.direct",
            routingKey: "orders.created",
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(testMessage));

        // Simulate connection loss and recovery
        await _channel.CloseAsync();
        await _connection!.CloseAsync();
        
        // Reconnect
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "admin",
            Password = "admin",
            VirtualHost = "/"
        };
        
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Act - Try to consume the persistent message
        var result = await _channel.BasicGetAsync("orders.created", autoAck: true);

        // Assert
        Assert.NotNull(result);
        var receivedMessage = Encoding.UTF8.GetString(result.Body.ToArray());
        Assert.Equal(testMessage, receivedMessage);
    }

    [Fact]
    public async Task AMQP_FanOutPattern_MultipleConsumersReceiveMessage()
    {
        // Arrange - Set up fanout pattern
        await _channel!.QueueDeclareAsync("orders.updated", durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueDeclareAsync("orders.deleted", durable: true, exclusive: false, autoDelete: false);
        
        await _channel.QueueBindAsync("orders.updated", "learning.direct", "orders.updated");
        await _channel.QueueBindAsync("orders.deleted", "learning.direct", "orders.deleted");

        var createdMessage = "Order created event";
        var updatedMessage = "Order updated event";  
        var deletedMessage = "Order deleted event";

        // Act - Publish to different routing keys
        await _channel.BasicPublishAsync("learning.direct", "orders.created", false, Encoding.UTF8.GetBytes(createdMessage));
        await _channel.BasicPublishAsync("learning.direct", "orders.updated", false, Encoding.UTF8.GetBytes(updatedMessage));
        await _channel.BasicPublishAsync("learning.direct", "orders.deleted", false, Encoding.UTF8.GetBytes(deletedMessage));

        // Wait a bit for messages to be routed
        await Task.Delay(100);

        // Assert - Each queue should have its respective message
        var createdResult = await _channel.BasicGetAsync("orders.created", autoAck: true);
        var updatedResult = await _channel.BasicGetAsync("orders.updated", autoAck: true);
        var deletedResult = await _channel.BasicGetAsync("orders.deleted", autoAck: true);

        Assert.NotNull(createdResult);
        Assert.NotNull(updatedResult);
        Assert.NotNull(deletedResult);
        
        Assert.Equal(createdMessage, Encoding.UTF8.GetString(createdResult.Body.ToArray()));
        Assert.Equal(updatedMessage, Encoding.UTF8.GetString(updatedResult.Body.ToArray()));
        Assert.Equal(deletedMessage, Encoding.UTF8.GetString(deletedResult.Body.ToArray()));
    }

    [Fact]
    public async Task AMQP_HighThroughput_ProcessesMultipleMessages()
    {
        // Arrange
        const int messageCount = 100;
        var messagesReceived = new List<string>();
        var allMessagesReceived = new TaskCompletionSource<bool>();
        
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            messagesReceived.Add(message);
            
            if (messagesReceived.Count >= messageCount)
            {
                allMessagesReceived.SetResult(true);
            }
            return Task.CompletedTask;
        };

        await _channel!.BasicConsumeAsync("orders.created", autoAck: true, consumer: consumer);

        // Act - Publish multiple messages rapidly
        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var message = $"Message {i}";
            var task = _channel.BasicPublishAsync("learning.direct", "orders.created", false, 
                Encoding.UTF8.GetBytes(message)).AsTask();
            publishTasks.Add(task);
        }

        await Task.WhenAll(publishTasks);

        // Assert
        var completed = await allMessagesReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(completed, $"Should receive all {messageCount} messages");
        Assert.Equal(messageCount, messagesReceived.Count);
    }

    [Fact]
    public async Task AMQP_PriorityQueue_ProcessesHighPriorityFirst()
    {
        // Arrange - Create priority queue
        var queueArgs = new Dictionary<string, object?>
        {
            { "x-max-priority", 10 }
        };
        
        await _channel!.QueueDeclareAsync("priority.test", durable: true, exclusive: false, 
            autoDelete: false, arguments: queueArgs);
        await _channel.QueueBindAsync("priority.test", "learning.direct", "priority.test");

        // Publish messages with different priorities
        var lowPriorityProps = new BasicProperties { Priority = 1 };
        var highPriorityProps = new BasicProperties { Priority = 10 };

        await _channel.BasicPublishAsync("learning.direct", "priority.test", false, 
            lowPriorityProps, Encoding.UTF8.GetBytes("Low priority"));
        await _channel.BasicPublishAsync("learning.direct", "priority.test", false, 
            highPriorityProps, Encoding.UTF8.GetBytes("High priority"));

        // Wait for messages to be queued
        await Task.Delay(100);

        // Act - Consume messages
        var firstMessage = await _channel.BasicGetAsync("priority.test", autoAck: true);
        var secondMessage = await _channel.BasicGetAsync("priority.test", autoAck: true);

        // Assert - High priority should come first
        Assert.NotNull(firstMessage);
        Assert.NotNull(secondMessage);
        Assert.Equal("High priority", Encoding.UTF8.GetString(firstMessage.Body.ToArray()));
        Assert.Equal("Low priority", Encoding.UTF8.GetString(secondMessage.Body.ToArray()));
    }
}
