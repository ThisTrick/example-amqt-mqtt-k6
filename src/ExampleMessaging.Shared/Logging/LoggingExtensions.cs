using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace ExampleMessaging.Shared.Logging;

/// <summary>
/// Extensions for configuring structured logging with Serilog across all services
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configure Serilog with structured logging, correlation IDs, and appropriate sinks
    /// </summary>
    public static void ConfigureSerilog(this IHostBuilder hostBuilder, string serviceName)
    {
        hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(
                    new JsonFormatter(),
                    $"logs/{serviceName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true
                );

            // Add performance logging for development
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug();
            }
        });
    }

    /// <summary>
    /// Add correlation ID enrichment for request tracking
    /// </summary>
    public static IServiceCollection AddCorrelationLogging(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        return services;
    }
}

/// <summary>
/// Interface for correlation context tracking
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}

/// <summary>
/// Implementation of correlation context for request tracking
/// </summary>
public class CorrelationContext : ICorrelationContext
{
    private string _correlationId = Guid.NewGuid().ToString();

    public string CorrelationId => _correlationId;

    public void SetCorrelationId(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            _correlationId = correlationId;
        }
    }
}