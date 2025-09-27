# Research: .NET Messaging Learning Platform

## Technical Decisions

### .NET Messaging Libraries

**AMQP Library Decision**: RabbitMQ.Client v6.8.1
- **Rationale**: Official RabbitMQ library with comprehensive AMQP 0.9.1 support, mature async patterns, excellent documentation
- **Alternatives considered**: 
  - EasyNetQ (too high-level, hides protocol details needed for learning)
  - Apache Queueing Interop API (incomplete .NET support)
- **Learning benefit**: Direct protocol exposure teaches AMQP fundamentals

**MQTT Library Decision**: MQTTnet v4.3.3
- **Rationale**: Leading .NET MQTT library, supports MQTT 3.1.1 and 5.0, excellent async/await patterns, active community
- **Alternatives considered**:
  - System.Net.Mqtt (discontinued)
  - Eclipse Paho .NET (limited feature set)
- **Learning benefit**: Comprehensive QoS levels, retain messages, will topics demonstration

### Performance Testing Strategy

**k6 Performance Testing**: JavaScript-based scenarios for protocol validation
- **API Load Testing**: HTTP endpoints under concurrent load (baseline: 100 req/s)
- **Message Broker Testing**: Protocol-specific load patterns (baseline: 1000 msg/s)
- **Metrics Collection**: Response times, throughput, error rates, resource utilization

**Educational Performance Targets**:
- **API Endpoints**: 100 requests/second sustained (learning baseline)
- **AMQP Messages**: 1000 messages/second through RabbitMQ queues
- **MQTT Messages**: 1000 messages/second through MQTT topics
- **Latency Expectations**: <100ms p95 for API, <50ms for message processing

### Container Architecture

**Docker Compose Services**:
- **RabbitMQ**: Management plugin enabled, AMQP + MQTT protocols
- **Grafana**: Performance dashboards with protocol-specific metrics
- **Publisher API**: HTTP service exposing AMQP/MQTT publishing endpoints
- **AMQP Consumer**: Background service processing queue messages
- **MQTT Consumer**: Background service subscribing to topics

### Educational Documentation Structure

**Ukrainian Language Documentation**:
- **Protocol Concepts**: AMQP exchanges/queues vs MQTT topics/subscriptions
- **Connection Management**: Retry logic, circuit breakers, graceful shutdowns  
- **Message Patterns**: Request/Reply, Pub/Sub, Fan-Out, Competing Consumers
- **Error Scenarios**: Broker unavailable, consumer failures, message persistence
- **Performance Analysis**: Interpreting k6 results, bottleneck identification

### TDD Integration Testing Strategy

**Testcontainers Integration**: Real RabbitMQ containers for protocol validation
- **AMQP Integration Tests**: Queue declaration, message publishing/consuming, acknowledgments
- **MQTT Integration Tests**: Topic publishing/subscribing, QoS levels, retained messages
- **API Contract Tests**: OpenAPI schema validation, error response testing

**Test Scenarios**:
- Connection establishment and recovery
- Message persistence and delivery guarantees
- Protocol-specific features (routing keys, topic filters)
- Performance under load conditions
- Error handling and retry mechanisms

## Implementation Approach

### Phase 1 Priority: Educational Value
- Start with simplest pub/sub examples
- Progress to advanced patterns (request/reply, fan-out)
- Demonstrate error handling and recovery
- Show performance monitoring and optimization

### Logging and Observability
- **Structured Logging**: Serilog with correlation IDs across services
- **Metrics Collection**: Protocol-specific counters (messages sent/received, connections)
- **Grafana Dashboards**: Visual representation of system behavior under load

### CLI Management Tools
- Environment startup/shutdown commands
- Test scenario execution
- Performance report generation
- Service health monitoring

## Research Conclusions

All technical decisions align with constitutional requirements:
- ✅ Protocol learning focus through direct library usage
- ✅ .NET best practices with modern async patterns
- ✅ TDD with Testcontainers for real protocol testing
- ✅ Docker containerization for consistent environment
- ✅ Performance observability with k6 and Grafana
- ✅ Ukrainian documentation for educational purposes

No blocking unknowns remain. Performance targets established for educational context rather than production scale.
