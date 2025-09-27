using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ExampleMessaging.ContractTests;

public class MqttPublishContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MqttPublishContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_MqttPublish_ValidRequest_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            payload = "Test MQTT message",
            topic = "sensors/temperature/room1",
            qosLevel = 1,
            retain = false,
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because the endpoint doesn't exist yet
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("messageId", out var messageId));
        Assert.False(string.IsNullOrEmpty(messageId.GetString()));
    }

    [Fact]
    public async Task POST_MqttPublish_MissingTopic_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            payload = "Test message",
            qosLevel = 1,
            messageType = "Event"
            // Missing topic
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_MqttPublish_InvalidQosLevel_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            payload = "Test message",
            topic = "sensors/temperature/room1",
            qosLevel = 99, // Invalid QoS level
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_MqttPublish_InvalidTopicFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            payload = "Test message",
            topic = "sensors/#/invalid", // Invalid topic with wildcard in middle
            qosLevel = 1,
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_MqttPublish_ResponseMatchesOpenApiContract()
    {
        // Arrange
        var request = new
        {
            payload = "MQTT contract validation test",
            topic = "test/contract/validation",
            qosLevel = 2,
            retain = true,
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because the response structure doesn't exist yet
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Validate response structure matches OpenAPI contract
        Assert.True(result.TryGetProperty("messageId", out var messageId));
        Assert.True(result.TryGetProperty("timestamp", out var timestamp));
        Assert.True(result.TryGetProperty("status", out var status));
        
        Assert.Equal("accepted", status.GetString());
        Assert.True(Guid.TryParse(messageId.GetString(), out _));
        Assert.True(DateTime.TryParse(timestamp.GetString(), out _));
    }

    [Fact]
    public async Task POST_MqttPublish_WithRetainFlag_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            payload = "Retained MQTT message",
            topic = "sensors/last-known/temperature",
            qosLevel = 1,
            retain = true,
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mqtt/publish", request);

        // Assert - This MUST FAIL because the endpoint doesn't exist yet
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("messageId", out var messageId));
        Assert.True(result.TryGetProperty("retained", out var retained));
        Assert.True(retained.GetBoolean());
    }
}
