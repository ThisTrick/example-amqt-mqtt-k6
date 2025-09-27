using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ExampleMessaging.ContractTests;

public class AmqpPublishContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AmqpPublishContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_AmqpPublish_ValidRequest_ReturnsAccepted()
    {
        // Arrange
        var request = new
        {
            payload = "Test AMQP message",
            exchange = "learning.direct",
            routingKey = "orders.created",
            messageType = "Event",
            persistent = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/amqp/publish", request);

        // Assert - This MUST FAIL because the endpoint doesn't exist yet
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("messageId", out var messageId));
        Assert.False(string.IsNullOrEmpty(messageId.GetString()));
    }

    [Fact]
    public async Task POST_AmqpPublish_MissingPayload_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            exchange = "learning.direct",
            routingKey = "orders.created",
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/amqp/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_AmqpPublish_InvalidExchange_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            payload = "Test message",
            exchange = "", // Invalid empty exchange
            routingKey = "orders.created",
            messageType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/amqp/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_AmqpPublish_InvalidMessageType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            payload = "Test message",
            exchange = "learning.direct",
            routingKey = "orders.created",
            messageType = "InvalidType" // Invalid message type
        };

        // Act
        var response = await _client.PostAsJsonAsync("/amqp/publish", request);

        // Assert - This MUST FAIL because validation doesn't exist yet
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_AmqpPublish_ResponseMatchesOpenApiContract()
    {
        // Arrange
        var request = new
        {
            payload = "Contract validation test",
            exchange = "learning.direct",
            routingKey = "test.contract",
            messageType = "Event",
            persistent = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/amqp/publish", request);

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
}
