using System.Net;
using System.Text.Json;
using ExampleMessaging.Shared.Logging;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ExampleMessaging.Publisher.Api.Middleware;

/// <summary>
/// Global exception handling middleware to catch unhandled exceptions and return structured responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly ICorrelationContext _correlationContext;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        ICorrelationContext correlationContext)
    {
        _next = next;
        _logger = logger;
        _correlationContext = correlationContext;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred. CorrelationId: {CorrelationId}", 
                _correlationContext.CorrelationId);

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ArgumentException argumentException => new ErrorResponse
            {
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = argumentException.Message,
                CorrelationId = _correlationContext.CorrelationId
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "Access denied",
                CorrelationId = _correlationContext.CorrelationId
            },
            NotImplementedException => new ErrorResponse
            {
                Title = "Not Implemented",
                Status = (int)HttpStatusCode.NotImplemented,
                Detail = "This feature is not yet implemented",
                CorrelationId = _correlationContext.CorrelationId
            },
            TimeoutException => new ErrorResponse
            {
                Title = "Gateway Timeout",
                Status = (int)HttpStatusCode.GatewayTimeout,
                Detail = "The operation timed out",
                CorrelationId = _correlationContext.CorrelationId
            },
            _ => new ErrorResponse
            {
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An unexpected error occurred",
                CorrelationId = _correlationContext.CorrelationId
            }
        };

        context.Response.StatusCode = response.Status;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Structured error response following RFC 7807 Problem Details
/// </summary>
public class ErrorResponse
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Instance => $"/error/{Status}";
    public string Type => $"https://httpstatuses.com/{Status}";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}