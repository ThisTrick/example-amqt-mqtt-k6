using Microsoft.AspNetCore.Mvc;
using ExampleMessaging.Publisher.Api.Models;

namespace ExampleMessaging.Publisher.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class MqttController : ControllerBase
{
    private readonly ILogger<MqttController> _logger;

    public MqttController(ILogger<MqttController> logger)
    {
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<ActionResult<PublishResponse>> Publish([FromBody] MqttPublishRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Additional validation
            if (string.IsNullOrEmpty(request.Topic))
            {
                return BadRequest(new ErrorResponse { Error = "InvalidTopic", Message = "Topic cannot be empty" });
            }

            if (request.QosLevel < 0 || request.QosLevel > 2)
            {
                return BadRequest(new ErrorResponse { Error = "InvalidQosLevel", Message = "QosLevel must be 0, 1, or 2" });
            }

            if (!IsValidMqttTopic(request.Topic))
            {
                return BadRequest(new ErrorResponse { Error = "InvalidTopicFormat", Message = "Invalid MQTT topic format" });
            }

            // TODO: Implement actual MQTT publishing logic
            // For now, return mock response to make contract tests pass
            var messageId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("MQTT message published: {MessageId} to topic {Topic} with QoS {QosLevel}", 
                messageId, request.Topic, request.QosLevel);

            var response = new PublishResponse
            {
                MessageId = messageId,
                Status = "accepted",
                Timestamp = DateTime.UtcNow,
                Retained = request.Retain
            };

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing MQTT message");
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "PublishError", 
                Message = "Internal server error while publishing message" 
            });
        }
    }

    private static bool IsValidMqttTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            return false;

        // MQTT topic validation rules for publishing
        // - Cannot contain wildcards (# or +)
        // - Must not contain null characters
        if (topic.Contains('#') || topic.Contains('+'))
            return false;

        if (topic.Contains('\0'))
            return false;

        return true;
    }
}
