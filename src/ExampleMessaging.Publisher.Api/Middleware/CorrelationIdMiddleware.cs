using ExampleMessaging.Shared.Logging;
using Serilog.Context;

namespace ExampleMessaging.Publisher.Api.Middleware;

/// <summary>
/// Middleware to handle correlation ID tracking across requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        // Extract correlation ID from header or generate new one
        var correlationId = ExtractCorrelationId(context);
        correlationContext.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Add correlation ID to Serilog context for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ExtractCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request headers
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        // If not present, generate a new one
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        return correlationId;
    }
}

/// <summary>
/// Extension methods for adding correlation middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Add correlation ID middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
