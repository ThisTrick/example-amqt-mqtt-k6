using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using ExampleMessaging.Mqtt.Consumer.Services;
using ExampleMessaging.Mqtt.Consumer.Handlers;

namespace ExampleMessaging.Mqtt.Consumer;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting MQTT Consumer application");

            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "MQTT Consumer application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<ExampleMessaging.Shared.Configuration.MqttConsumerConfig>(
                    context.Configuration.GetSection("MqttConsumerConfig"));

                // Register services
                services.AddScoped<MqttMessageHandler>();
                services.AddHostedService<MqttConnectionService>();
            });
}
