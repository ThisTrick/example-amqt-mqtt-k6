using System.CommandLine;
using ExampleMessaging.Cli.Commands;

namespace ExampleMessaging.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("ExampleMessaging CLI - Tools for managing AMQP and MQTT messaging infrastructure")
        {
            StartCommand.Create(),
            TestCommand.Create(), 
            StatusCommand.Create()
        };

        rootCommand.Description = """
            ExampleMessaging CLI provides tools for:
            • Starting and managing messaging infrastructure (Docker Compose)
            • Publishing test messages to AMQP/MQTT brokers
            • Monitoring service health and status
            
            Use --help with any command to see detailed options.
            """;

        try
        {
            var parseResult = rootCommand.Parse(args);
            return await parseResult.InvokeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            Console.WriteLine("Use --help for usage information.");
            return 1;
        }
    }
}
