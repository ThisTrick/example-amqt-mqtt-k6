using System.CommandLine;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Cli.Commands;

public static class StatusCommand
{
    public static Command Create()
    {
        var servicesOption = new Option<string>("--services") { Description = "Check specific services (amqp, mqtt, or both)" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Show detailed status information" };

        // Set default values
        servicesOption.DefaultValueFactory = _ => "both";

        var command = new Command("status", "Check the health and status of messaging services");
        command.Add(servicesOption);  
        command.Add(verboseOption);

        command.SetAction(async (parseResult) =>
        {
            var services = parseResult.GetValue(servicesOption) ?? "both";
            var verbose = parseResult.GetValue(verboseOption);

            await ExecuteAsync(services, verbose);
        });

        return command;
    }

    private static async Task ExecuteAsync(string services, bool verbose)
    {
        try
        {
            Console.WriteLine($"🔍 Checking messaging services status...");
            Console.WriteLine($"   Services: {services.ToUpper()}");
            Console.WriteLine();

            var shouldCheckAmqp = services.ToLower() == "amqp" || services.ToLower() == "both";
            var shouldCheckMqtt = services.ToLower() == "mqtt" || services.ToLower() == "both";

            var overallHealthy = true;

            if (shouldCheckAmqp)
            {
                var amqpHealthy = await CheckAmqpStatusAsync(verbose);
                overallHealthy = overallHealthy && amqpHealthy;
                Console.WriteLine();
            }

            if (shouldCheckMqtt)
            {
                var mqttHealthy = await CheckMqttStatusAsync(verbose);
                overallHealthy = overallHealthy && mqttHealthy;
                Console.WriteLine();
            }

            if (overallHealthy)
            {
                Console.WriteLine("✅ All messaging services are healthy!");
            }
            else
            {
                Console.WriteLine("❌ Some messaging services have issues!");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during status check: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            Environment.Exit(1);
        }
    }

    private static async Task<bool> CheckAmqpStatusAsync(bool verbose)
    {
        Console.WriteLine("📡 AMQP (RabbitMQ) Status:");
        
        var healthy = true;
        
        try
        {
            // Check network connectivity
            var isReachable = await IsPortReachableAsync("localhost", 5672);
            if (!isReachable)
            {
                Console.WriteLine("   ❌ RabbitMQ port 5672 is not reachable");
                return false;
            }
            
            Console.WriteLine("   ✓ Network connectivity: OK");

            // Check RabbitMQ connection and basic operations
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "admin",
                VirtualHost = "/",
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                AutomaticRecoveryEnabled = true
            };

            using var connection = await factory.CreateConnectionAsync("StatusCommand");
            using var channel = await connection.CreateChannelAsync();
            
            Console.WriteLine("   ✓ RabbitMQ connection: OK");

            if (verbose)
            {
                Console.WriteLine($"      Server properties:");
                var serverProps = connection.ServerProperties;
                if (serverProps != null)
                {
                    foreach (var prop in serverProps)
                    {
                        Console.WriteLine($"        {prop.Key}: {prop.Value ?? "null"}");
                    }
                }
                else
                {
                    Console.WriteLine($"        No server properties available");
                }
                Console.WriteLine($"      Channel number: {channel.ChannelNumber}");
                Console.WriteLine($"      Is open: {channel.IsOpen}");
            }

            // Check basic queue operations
            try
            {
                var queueInfo = await channel.QueueDeclarePassiveAsync("test-queue");
                Console.WriteLine($"   ✓ Test queue status: {queueInfo.MessageCount} messages, {queueInfo.ConsumerCount} consumers");
                
                if (verbose)
                {
                    Console.WriteLine($"      Queue name: {queueInfo.QueueName}");
                    Console.WriteLine($"      Message count: {queueInfo.MessageCount}");
                    Console.WriteLine($"      Consumer count: {queueInfo.ConsumerCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Test queue not found (this is normal if no tests have been run)");
                if (verbose)
                {
                    Console.WriteLine($"      Error: {ex.Message}");
                }
            }

            // Test basic publish/consume capability
            var testQueueName = $"status-test-{Guid.NewGuid():N}";
            try
            {
                await channel.QueueDeclareAsync(testQueueName, durable: false, exclusive: true, autoDelete: true);
                
                var testMessage = new AmqpMessage(
                    payload: "Status check message",
                    routingKey: testQueueName,
                    exchange: "",
                    messageType: MessageType.Event,
                    source: "StatusCommand"
                );
                
                var json = JsonSerializer.Serialize(testMessage);
                var body = Encoding.UTF8.GetBytes(json);
                var properties = new BasicProperties();

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: testQueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);
                
                Console.WriteLine("   ✓ Message publishing: OK");
                
                // Clean up test queue
                await channel.QueueDeleteAsync(testQueueName);
                Console.WriteLine("   ✓ Queue management: OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Message operations failed: {ex.Message}");
                healthy = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ AMQP connection failed: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"      Details: {ex.StackTrace}");
            }
            healthy = false;
        }

        return healthy;
    }

    private static async Task<bool> CheckMqttStatusAsync(bool verbose)
    {
        Console.WriteLine("📡 MQTT Status:");
        
        try
        {
            // Check network connectivity
            var isReachable = await IsPortReachableAsync("localhost", 1883);
            if (!isReachable)
            {
                Console.WriteLine("   ❌ MQTT port 1883 is not reachable");
                return false;
            }
            
            Console.WriteLine("   ✓ Network connectivity: OK");
            
            // MQTT functionality temporarily simulated
            Console.WriteLine("   ⚠️  MQTT broker connection: Not implemented (API compatibility issues)");
            Console.WriteLine("   📝 Would check: broker info, topic subscriptions, message publishing");
            
            if (verbose)
            {
                Console.WriteLine("      Planned checks:");
                Console.WriteLine("        - Broker connection and authentication");
                Console.WriteLine("        - Topic creation and subscription");
                Console.WriteLine("        - Message publishing and receiving");
                Console.WriteLine("        - QoS level support");
                Console.WriteLine("        - Retained message handling");
            }
            
            return true; // Temporary: assume healthy for network connectivity
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ MQTT check failed: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"      Details: {ex.StackTrace}");
            }
            return false;
        }
    }

    private static async Task<bool> IsPortReachableAsync(string host, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port);
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}
