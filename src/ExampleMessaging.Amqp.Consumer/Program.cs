using ExampleMessaging.Amqp.Consumer.Handlers;
using ExampleMessaging.Amqp.Consumer.Services;
using ExampleMessaging.Shared.Configuration;
using ExampleMessaging.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting AMQP Consumer application");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Configure AMQP Consumer
    builder.Services.Configure<AmqpConsumerConfig>(
        builder.Configuration.GetSection("AmqpConsumer"));

    // Register services
    builder.Services.AddScoped<IAmqpMessageHandler, AmqpMessageHandler>();
    builder.Services.AddSingleton<IAmqpConnectionService, AmqpConnectionService>();
    builder.Services.AddHostedService<AmqpConnectionService>(provider => 
        (AmqpConnectionService)provider.GetRequiredService<IAmqpConnectionService>());

    var host = builder.Build();

    // Log startup information
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("AMQP Consumer configured and ready to start");
    logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
    logger.LogInformation("Application: {ApplicationName}", builder.Environment.ApplicationName);

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AMQP Consumer application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.Information("AMQP Consumer application shutting down");
    await Log.CloseAndFlushAsync();
}
