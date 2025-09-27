using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ExampleMessaging.ContractTests;

public class MetricsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MetricsContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_Metrics_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert - This MUST FAIL because the endpoint doesn't exist yet
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GET_Metrics_ResponseMatchesOpenApiContract()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert - This MUST FAIL because the response structure doesn't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Validate response structure matches OpenAPI contract
        Assert.True(result.TryGetProperty("timestamp", out var timestamp));
        Assert.True(result.TryGetProperty("amqp", out var amqp));
        Assert.True(result.TryGetProperty("mqtt", out var mqtt));
        Assert.True(result.TryGetProperty("system", out var system));
        
        // Validate timestamp format
        Assert.True(DateTime.TryParse(timestamp.GetString(), out _));
        
        // Validate AMQP metrics structure
        Assert.Equal(JsonValueKind.Object, amqp.ValueKind);
        Assert.True(amqp.TryGetProperty("messagesPublished", out var amqpPublished));
        Assert.True(amqp.TryGetProperty("messagesConsumed", out var amqpConsumed));
        Assert.True(amqp.TryGetProperty("connectionCount", out var amqpConnections));
        Assert.True(amqp.TryGetProperty("averageLatency", out var amqpLatency));
        
        // Validate MQTT metrics structure
        Assert.Equal(JsonValueKind.Object, mqtt.ValueKind);
        Assert.True(mqtt.TryGetProperty("messagesPublished", out var mqttPublished));
        Assert.True(mqtt.TryGetProperty("messagesConsumed", out var mqttConsumed));
        Assert.True(mqtt.TryGetProperty("connectionCount", out var mqttConnections));
        Assert.True(mqtt.TryGetProperty("averageLatency", out var mqttLatency));
        
        // Validate System metrics structure
        Assert.Equal(JsonValueKind.Object, system.ValueKind);
        Assert.True(system.TryGetProperty("memoryUsage", out var memoryUsage));
        Assert.True(system.TryGetProperty("cpuUsage", out var cpuUsage));
        Assert.True(system.TryGetProperty("uptime", out var uptime));
        
        // Validate all numeric metrics are numbers
        Assert.True(amqpPublished.ValueKind == JsonValueKind.Number);
        Assert.True(amqpConsumed.ValueKind == JsonValueKind.Number);
        Assert.True(amqpConnections.ValueKind == JsonValueKind.Number);
        Assert.True(amqpLatency.ValueKind == JsonValueKind.Number);
        
        Assert.True(mqttPublished.ValueKind == JsonValueKind.Number);
        Assert.True(mqttConsumed.ValueKind == JsonValueKind.Number);
        Assert.True(mqttConnections.ValueKind == JsonValueKind.Number);
        Assert.True(mqttLatency.ValueKind == JsonValueKind.Number);
        
        Assert.True(memoryUsage.ValueKind == JsonValueKind.Number);
        Assert.True(cpuUsage.ValueKind == JsonValueKind.Number);
        Assert.True(uptime.ValueKind == JsonValueKind.Number);
    }

    [Fact]
    public async Task GET_Metrics_ValidatesMetricRanges()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert - This MUST FAIL because metric validation doesn't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("amqp", out var amqp));
        Assert.True(result.TryGetProperty("mqtt", out var mqtt));
        Assert.True(result.TryGetProperty("system", out var system));
        
        // Validate AMQP metric ranges
        Assert.True(amqp.TryGetProperty("messagesPublished", out var amqpPublished));
        Assert.True(amqp.TryGetProperty("messagesConsumed", out var amqpConsumed));
        Assert.True(amqp.TryGetProperty("connectionCount", out var amqpConnections));
        Assert.True(amqp.TryGetProperty("averageLatency", out var amqpLatency));
        
        Assert.True(amqpPublished.GetInt64() >= 0);
        Assert.True(amqpConsumed.GetInt64() >= 0);
        Assert.True(amqpConnections.GetInt32() >= 0);
        Assert.True(amqpLatency.GetDouble() >= 0);
        
        // Validate MQTT metric ranges
        Assert.True(mqtt.TryGetProperty("messagesPublished", out var mqttPublished));
        Assert.True(mqtt.TryGetProperty("messagesConsumed", out var mqttConsumed));
        Assert.True(mqtt.TryGetProperty("connectionCount", out var mqttConnections));
        Assert.True(mqtt.TryGetProperty("averageLatency", out var mqttLatency));
        
        Assert.True(mqttPublished.GetInt64() >= 0);
        Assert.True(mqttConsumed.GetInt64() >= 0);
        Assert.True(mqttConnections.GetInt32() >= 0);
        Assert.True(mqttLatency.GetDouble() >= 0);
        
        // Validate System metric ranges
        Assert.True(system.TryGetProperty("memoryUsage", out var memoryUsage));
        Assert.True(system.TryGetProperty("cpuUsage", out var cpuUsage));
        Assert.True(system.TryGetProperty("uptime", out var uptime));
        
        Assert.True(memoryUsage.GetDouble() >= 0 && memoryUsage.GetDouble() <= 100);
        Assert.True(cpuUsage.GetDouble() >= 0 && cpuUsage.GetDouble() <= 100);
        Assert.True(uptime.GetInt64() >= 0);
    }

    [Fact]
    public async Task GET_Metrics_IncludesMessageRates()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert - This MUST FAIL because rate calculations don't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("amqp", out var amqp));
        Assert.True(result.TryGetProperty("mqtt", out var mqtt));
        
        // Validate rate metrics are included
        Assert.True(amqp.TryGetProperty("publishRate", out var amqpPublishRate));
        Assert.True(amqp.TryGetProperty("consumeRate", out var amqpConsumeRate));
        
        Assert.True(mqtt.TryGetProperty("publishRate", out var mqttPublishRate));
        Assert.True(mqtt.TryGetProperty("consumeRate", out var mqttConsumeRate));
        
        // Rates should be numbers (messages per second)
        Assert.True(amqpPublishRate.ValueKind == JsonValueKind.Number);
        Assert.True(amqpConsumeRate.ValueKind == JsonValueKind.Number);
        Assert.True(mqttPublishRate.ValueKind == JsonValueKind.Number);
        Assert.True(mqttConsumeRate.ValueKind == JsonValueKind.Number);
        
        // Rates should be non-negative
        Assert.True(amqpPublishRate.GetDouble() >= 0);
        Assert.True(amqpConsumeRate.GetDouble() >= 0);
        Assert.True(mqttPublishRate.GetDouble() >= 0);
        Assert.True(mqttConsumeRate.GetDouble() >= 0);
    }

    [Fact]
    public async Task GET_Metrics_IncludesErrorCounts()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert - This MUST FAIL because error tracking doesn't exist yet
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("amqp", out var amqp));
        Assert.True(result.TryGetProperty("mqtt", out var mqtt));
        
        // Validate error metrics are included
        Assert.True(amqp.TryGetProperty("publishErrors", out var amqpPublishErrors));
        Assert.True(amqp.TryGetProperty("consumeErrors", out var amqpConsumeErrors));
        
        Assert.True(mqtt.TryGetProperty("publishErrors", out var mqttPublishErrors));
        Assert.True(mqtt.TryGetProperty("consumeErrors", out var mqttConsumeErrors));
        
        // Error counts should be numbers
        Assert.True(amqpPublishErrors.ValueKind == JsonValueKind.Number);
        Assert.True(amqpConsumeErrors.ValueKind == JsonValueKind.Number);
        Assert.True(mqttPublishErrors.ValueKind == JsonValueKind.Number);
        Assert.True(mqttConsumeErrors.ValueKind == JsonValueKind.Number);
        
        // Error counts should be non-negative integers
        Assert.True(amqpPublishErrors.GetInt64() >= 0);
        Assert.True(amqpConsumeErrors.GetInt64() >= 0);
        Assert.True(mqttPublishErrors.GetInt64() >= 0);
        Assert.True(mqttConsumeErrors.GetInt64() >= 0);
    }
}
