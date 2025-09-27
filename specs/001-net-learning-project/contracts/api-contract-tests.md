# Contract Tests: Publisher API

## Test Scenarios

### AMQP Publishing Contract Tests

#### POST /amqp/publish - Valid Request
```json
Request:
{
  "payload": "Test message for AMQP",
  "exchange": "learning.direct",
  "routingKey": "test.message",
  "correlationId": "123e4567-e89b-12d3-a456-426614174000",
  "messageType": "Event",
  "priority": 1,
  "persistent": true,
  "expiration": 60000
}

Expected Response: 202 Accepted
{
  "messageId": "uuid",
  "correlationId": "123e4567-e89b-12d3-a456-426614174000", 
  "timestamp": "2025-09-27T12:00:00Z",
  "status": "Accepted"
}
```

#### POST /amqp/publish - Missing Required Fields
```json
Request:
{
  "payload": "Test message"
  // Missing exchange and routingKey
}

Expected Response: 400 Bad Request
{
  "error": "Validation failed",
  "code": "VALIDATION_ERROR",
  "timestamp": "2025-09-27T12:00:00Z",
  "details": {
    "exchange": "Field is required",
    "routingKey": "Field is required"
  }
}
```

#### POST /amqp/publish - Invalid Exchange Pattern
```json
Request:
{
  "payload": "Test message",
  "exchange": "invalid exchange name!",
  "routingKey": "test"
}

Expected Response: 400 Bad Request
{
  "error": "Invalid exchange name format",
  "code": "VALIDATION_ERROR"
}
```

#### POST /amqp/publish - Broker Unavailable
```json
Request: Valid AMQP request

Expected Response: 503 Service Unavailable
{
  "error": "AMQP broker unavailable",
  "code": "BROKER_UNAVAILABLE",
  "timestamp": "2025-09-27T12:00:00Z"
}
```

### MQTT Publishing Contract Tests

#### POST /mqtt/publish - Valid Request
```json
Request:
{
  "payload": "Test message for MQTT",
  "topic": "sensors/temperature/room1",
  "correlationId": "123e4567-e89b-12d3-a456-426614174000",
  "messageType": "Event",
  "qosLevel": 1,
  "retain": false
}

Expected Response: 202 Accepted
{
  "messageId": "uuid",
  "correlationId": "123e4567-e89b-12d3-a456-426614174000",
  "timestamp": "2025-09-27T12:00:00Z", 
  "status": "Accepted"
}
```

#### POST /mqtt/publish - Invalid Topic Pattern
```json
Request:
{
  "payload": "Test message",
  "topic": "sensors/+/wildcard"  // Wildcards not allowed in publish
}

Expected Response: 400 Bad Request
{
  "error": "Invalid topic format for publishing",
  "code": "VALIDATION_ERROR"
}
```

#### POST /mqtt/publish - Invalid QoS Level
```json
Request:
{
  "payload": "Test message",
  "topic": "test/topic",
  "qosLevel": 3  // Invalid QoS level
}

Expected Response: 400 Bad Request
{
  "error": "Invalid QoS level. Must be 0, 1, or 2",
  "code": "VALIDATION_ERROR"
}
```

### Health Check Contract Tests

#### GET /health - All Services Healthy
```json
Expected Response: 200 OK
{
  "status": "Healthy",
  "timestamp": "2025-09-27T12:00:00Z",
  "services": {
    "amqp": {
      "status": "Connected",
      "lastCheck": "2025-09-27T12:00:00Z",
      "responseTime": 15.5
    },
    "mqtt": {
      "status": "Connected", 
      "lastCheck": "2025-09-27T12:00:00Z",
      "responseTime": 12.3
    }
  }
}
```

#### GET /health - AMQP Service Degraded
```json
Expected Response: 200 OK
{
  "status": "Degraded",
  "timestamp": "2025-09-27T12:00:00Z",
  "services": {
    "amqp": {
      "status": "Disconnected",
      "lastCheck": "2025-09-27T11:59:30Z",
      "responseTime": null
    },
    "mqtt": {
      "status": "Connected",
      "lastCheck": "2025-09-27T12:00:00Z", 
      "responseTime": 18.7
    }
  }
}
```

### Metrics Contract Tests

#### GET /metrics - Current System Metrics
```json
Expected Response: 200 OK
{
  "timestamp": "2025-09-27T12:00:00Z",
  "http": {
    "requestsPerSecond": 25.5,
    "averageResponseTime": 45.2,
    "errorRate": 0.02
  },
  "amqp": {
    "messagesPerSecond": 150.0,
    "activeConnections": 3,
    "queueDepth": 12
  },
  "mqtt": {
    "messagesPerSecond": 200.0,
    "activeConnections": 5,
    "subscriptions": 8
  },
  "system": {
    "cpuUsage": 15.5,
    "memoryUsage": 134217728,
    "diskUsage": 45.2
  }
}
```

## Test Implementation Notes

### Validation Requirements
- All timestamps must be ISO 8601 format with UTC timezone
- UUIDs must follow RFC 4122 format
- Response times measured in milliseconds
- Error responses must include correlation ID when available
- Memory usage reported in bytes

### TDD Test Structure
1. **Arrange**: Set up test data and mock dependencies
2. **Act**: Execute API call with test payload
3. **Assert**: Validate response schema, status codes, and business rules

### Integration Test Requirements  
- Use Testcontainers for real RabbitMQ broker testing
- Validate actual message publishing and consumption
- Test broker connection recovery scenarios
- Verify protocol-specific behavior (AMQP acks, MQTT QoS)

### Performance Contract Validation
- API response times under 100ms for 95th percentile
- Successful message publishing rate of 100 req/s minimum
- Health checks complete within 1 second
- Metrics endpoint responds within 500ms
