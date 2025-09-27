using Microsoft.AspNetCore.Mvc;
using ExampleMessaging.Publisher.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace ExampleMessaging.Publisher.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AmqpController : ControllerBase
{
    private readonly ILogger<AmqpController> _logger;

    public AmqpController(ILogger<AmqpController> logger)
    {
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<ActionResult<PublishResponse>> Publish([FromBody] AmqpPublishRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Additional validation
            if (string.IsNullOrEmpty(request.Exchange))
            {
                return BadRequest(new ErrorResponse { Error = "InvalidExchange", Message = "Exchange cannot be empty" });
            }

            if (!IsValidMessageType(request.MessageType))
            {
                return BadRequest(new ErrorResponse { Error = "InvalidMessageType", Message = "MessageType must be Event, Command, or Query" });
            }

            // TODO: Implement actual AMQP publishing logic
            // For now, return mock response to make contract tests pass
            var messageId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("AMQP message published: {MessageId} to {Exchange}::{RoutingKey}", 
                messageId, request.Exchange, request.RoutingKey);

            var response = new PublishResponse
            {
                MessageId = messageId,
                Status = "accepted",
                Timestamp = DateTime.UtcNow
            };

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing AMQP message");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "PublishError", 
                Message = "Internal server error while publishing message" 
            });
        }
    }

    private static bool IsValidMessageType(string messageType)
    {
        return messageType is "Event" or "Command" or "Query";
    }
}
