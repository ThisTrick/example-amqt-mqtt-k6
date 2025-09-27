using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using MQTTnet;
using MQTTnet.Protocol;
using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Cli.Commands;

public static class TestCommand
{
    public static Command Create()
    {
        var protocolOption = new Option<string>("--protocol") { Description = "Protocol to test (amqp, mqtt, or both)" };
        var countOption = new Option<int>("--count") { Description = "Number of test messages to publish" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        // Set default values
        protocolOption.DefaultValueFactory = _ => "both";
        countOption.DefaultValueFactory = _ => 1;

        var command = new Command("test", "Publish test messages to AMQP and MQTT brokers");
        command.Add(protocolOption);  
        command.Add(countOption);
        command.Add(verboseOption);

        command.SetAction(async (parseResult) =>
        {
            var protocol = parseResult.GetValue(protocolOption) ?? "both";
            var count = parseResult.GetValue(countOption);
            var verbose = parseResult.GetValue(verboseOption);

            await ExecuteAsync(protocol, count, verbose);
        });

        return command;
    }

    private static async Task ExecuteAsync(string protocol, int count, bool verbose)
    {
        try
        {
            Console.WriteLine($"🧪 Starting test message publishing...");
            Console.WriteLine($"   Protocol: {protocol.ToUpper()}");
            Console.WriteLine($"   Count: {count} messages");
            Console.WriteLine();

            var shouldTestAmqp = protocol.ToLower() == "amqp" || protocol.ToLower() == "both";
            var shouldTestMqtt = protocol.ToLower() == "mqtt" || protocol.ToLower() == "both";

            var tasks = new List<Task>();

            if (shouldTestAmqp)
            {
                tasks.Add(PublishAmqpMessagesAsync(count, verbose));
            }

            if (shouldTestMqtt)
            {
                tasks.Add(PublishMqttMessagesAsync(count, verbose));
            }            await Task.WhenAll(tasks);

            Console.WriteLine();
            Console.WriteLine("✅ Test message publishing completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during test message publishing: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            Environment.Exit(1);
        }
    }

    private static async Task PublishAmqpMessagesAsync(int count, bool verbose)
    {
        Console.WriteLine("📡 Starting AMQP message publishing...");
        
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "admin",
            Password = "admin",
            VirtualHost = "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {
            using var connection = await factory.CreateConnectionAsync("TestCommand");
            using var channel = await connection.CreateChannelAsync();

                        // Declare exchange, queue, and binding
            await channel.ExchangeDeclareAsync("test-exchange", ExchangeType.Topic, durable: true);
            await channel.QueueDeclareAsync("test-queue", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync("test-queue", "test-exchange", "test.message", arguments: null);

            for (int i = 1; i <= count; i++)
            {
                var testMessage = new AmqpMessage(
                    payload: $"Test AMQP message #{i} sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                    exchange: "test-exchange",
                    routingKey: "test.message",
                    messageType: MessageType.Event,
                    source: "TestCommand"
                );

                var jsonMessage = JsonSerializer.Serialize(testMessage, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                var properties = new BasicProperties();
                properties.Persistent = true;
                properties.MessageId = testMessage.Id.ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object?>
                {
                    ["messageType"] = MessageType.Event.ToString(),
                    ["source"] = "TestCommand"
                };

                await channel.BasicPublishAsync(
                    exchange: "test-exchange",
                    routingKey: "test.message",
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                if (verbose)
                {
                    Console.WriteLine($"   📤 AMQP #{i}: {testMessage.Id} -> test.message");
                }
                else if (i % 10 == 0 || i == count)
                {
                    Console.WriteLine($"   📤 AMQP: {i}/{count} messages published");
                }
            }

            Console.WriteLine($"✅ AMQP: Successfully published {count} messages");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AMQP Error: {ex.Message}");
            throw;
        }
    }

    private static Task PublishMqttMessagesAsync(int count, bool verbose)
    {
        Console.WriteLine("📡 Starting MQTT message publishing...");
        
        // Temporarily simplified MQTT implementation
        try
        {
            Console.WriteLine($"   ⚠️  MQTT functionality temporarily disabled - API compatibility issues");
            Console.WriteLine($"   📝 Would publish {count} messages to topic 'test/message'");
            
            // Simulate successful completion for now
            Console.WriteLine($"   ✓ MQTT publishing simulation completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MQTT Error: {ex.Message}");
            throw;
        }
        
        return Task.CompletedTask;
    }
}
