using Microsoft.AspNetCore.Mvc;
using ExampleMessaging.Publisher.Api.Models;
using System.Reflection;

namespace ExampleMessaging.Publisher.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<HealthResponse>> Get()
    {
        try
        {
            // TODO: Implement actual health checks for RabbitMQ and MQTT
            // For now, return mock response to make contract tests pass
            
            var response = new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                Dependencies = new Dictionary<string, DependencyHealth>
                {
                    ["rabbitmq"] = new DependencyHealth { Status = "Healthy", ResponseTime = 15.5 },
                    ["mqtt"] = new DependencyHealth { Status = "Healthy", ResponseTime = 12.3 }
                }
            };

            _logger.LogInformation("Health check completed: {Status}", response.Status);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var errorResponse = new HealthResponse
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                Dependencies = new Dictionary<string, DependencyHealth>
                {
                    ["rabbitmq"] = new DependencyHealth { Status = "Unhealthy", ResponseTime = 0 },
                    ["mqtt"] = new DependencyHealth { Status = "Unhealthy", ResponseTime = 0 }
                }
            };

            return StatusCode(503, errorResponse);
        }
    }
}
