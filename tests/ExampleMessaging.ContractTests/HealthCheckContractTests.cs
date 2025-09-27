using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ExampleMessaging.ContractTests;

public class HealthCheckContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_Health_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - This MUST FAIL because the endpoint doesn't exist yet
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("status", out var status));
        Assert.Equal("Healthy", status.GetString());
    }

    [Fact]
    public async Task GET_Health_ResponseMatchesOpenApiContract()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - This MUST FAIL because the response structure doesn't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Validate response structure matches OpenAPI contract
        Assert.True(result.TryGetProperty("status", out var status));
        Assert.True(result.TryGetProperty("timestamp", out var timestamp));
        Assert.True(result.TryGetProperty("version", out var version));
        Assert.True(result.TryGetProperty("dependencies", out var dependencies));
        
        // Validate status is one of expected values
        var statusValue = status.GetString();
        Assert.True(statusValue == "Healthy" || statusValue == "Degraded" || statusValue == "Unhealthy");
        
        // Validate timestamp format
        Assert.True(DateTime.TryParse(timestamp.GetString(), out _));
        
        // Validate version is not empty
        Assert.False(string.IsNullOrEmpty(version.GetString()));
        
        // Validate dependencies structure
        Assert.Equal(JsonValueKind.Object, dependencies.ValueKind);
    }

    [Fact]
    public async Task GET_Health_ValidatesDependencyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - This MUST FAIL because dependency checks don't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("dependencies", out var dependencies));
        
        // Should include RabbitMQ and MQTT broker status
        Assert.True(dependencies.TryGetProperty("rabbitmq", out var rabbitmq));
        Assert.True(dependencies.TryGetProperty("mqtt", out var mqtt));
        
        // Each dependency should have status and response time
        Assert.True(rabbitmq.TryGetProperty("status", out var rabbitmqStatus));
        Assert.True(rabbitmq.TryGetProperty("responseTime", out var rabbitmqResponseTime));
        
        Assert.True(mqtt.TryGetProperty("status", out var mqttStatus));
        Assert.True(mqtt.TryGetProperty("responseTime", out var mqttResponseTime));
        
        // Validate status values
        var rabbitmqStatusValue = rabbitmqStatus.GetString();
        var mqttStatusValue = mqttStatus.GetString();
        
        Assert.True(rabbitmqStatusValue == "Healthy" || rabbitmqStatusValue == "Unhealthy");
        Assert.True(mqttStatusValue == "Healthy" || mqttStatusValue == "Unhealthy");
        
        // Validate response times are numbers
        Assert.True(rabbitmqResponseTime.ValueKind == JsonValueKind.Number);
        Assert.True(mqttResponseTime.ValueKind == JsonValueKind.Number);
    }

    [Fact]
    public async Task GET_Health_WhenUnhealthy_ReturnsServiceUnavailable()
    {
        // This test simulates what happens when dependencies are down
        // We'll need to mock unhealthy dependencies in the implementation
        
        // For now, this test documents the expected behavior
        // It MUST FAIL because the endpoint and unhealthy state handling don't exist yet
        
        // Note: In real implementation, we would configure test services to simulate failure
        var response = await _client.GetAsync("/health");
        
        // This assertion will fail during TDD phase, which is expected
        // When dependencies are down, should return 503 Service Unavailable
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(result.TryGetProperty("status", out var status));
            Assert.Equal("Unhealthy", status.GetString());
        }
        else
        {
            // During TDD phase, this will likely return 404 or other error
            // This is expected and shows the test is properly written to fail first
            Assert.True(false, $"Expected ServiceUnavailable or proper health endpoint, got {response.StatusCode}");
        }
    }
}
