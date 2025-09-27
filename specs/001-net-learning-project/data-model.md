# Data Model: .NET Messaging Learning Platform

## Core Entities

### Message
Represents a data payload transmitted between services with metadata for traceability and routing.

**Properties**:
- `Id`: Unique message identifier (Guid)
- `Payload`: Message content (string/JSON)
- `Timestamp`: Creation timestamp (DateTimeOffset)
- `CorrelationId`: Request correlation identifier (Guid)
- `MessageType`: Classification (enum: Event, Command, Query)
- `Source`: Originating service identifier (string)
- `Routing`: Protocol-specific routing information

**State Transitions**:
- Created → Published → Delivered → Acknowledged
- Created → Published → Failed (with retry capability)

**Validation Rules**:
- Id must be unique and non-empty
- Payload size limited to 64KB for educational scenarios
- Timestamp must be UTC
- CorrelationId required for request/reply patterns

### AmqpMessage : Message
Extends base Message with AMQP-specific routing information.

**Additional Properties**:
- `Exchange`: Target exchange name (string)
- `RoutingKey`: AMQP routing key (string)
- `Priority`: Message priority level (byte, 0-255)
- `Persistent`: Durability flag (bool)
- `Expiration`: TTL in milliseconds (int?)

**Business Rules**:
- Exchange must exist before publishing
- RoutingKey determines queue routing via bindings
- Priority affects queue ordering (when supported)
- Persistent messages survive broker restarts

### MqttMessage : Message
Extends base Message with MQTT-specific topic and QoS information.

**Additional Properties**:  
- `Topic`: MQTT topic path (string)
- `QosLevel`: Quality of Service level (enum: AtMostOnce, AtLeastOnce, ExactlyOnce)
- `Retain`: Retained message flag (bool)
- `DupFlag`: Duplicate delivery flag (bool)

**Business Rules**:
- Topic must follow MQTT naming conventions (no wildcards in publish)
- QoS levels determine delivery guarantees
- Retained messages stored by broker for new subscribers
- DupFlag set by broker for QoS 1/2 retransmissions

### ConnectionInfo
Represents connection state and configuration for message broker interactions.

**Properties**:
- `ConnectionId`: Unique connection identifier (Guid)
- `Protocol`: Message protocol type (enum: AMQP, MQTT)
- `Host`: Broker hostname/IP (string)
- `Port`: Broker port number (int)
- `VirtualHost`: AMQP virtual host or MQTT client path (string)
- `ClientId`: Protocol-specific client identifier (string)
- `Status`: Connection state (enum: Disconnected, Connecting, Connected, Failed)
- `LastHeartbeat`: Last successful heartbeat (DateTimeOffset?)
- `RetryCount`: Failed connection attempt counter (int)

**State Transitions**:
- Disconnected → Connecting → Connected
- Connected → Failed → Disconnected (with retry logic)
- Connected → Disconnected (graceful shutdown)

### TestScenario
Defines performance test cases with specific load patterns and success criteria.

**Properties**:
- `ScenarioId`: Unique test scenario identifier (Guid)
- `Name`: Human-readable scenario name (string)
- `Description`: Scenario purpose and expectations (string)
- `Protocol`: Target protocol for testing (enum: HTTP_API, AMQP, MQTT)
- `LoadPattern`: Load generation configuration (LoadPatternConfig)
- `Duration`: Test execution duration (TimeSpan)
- `SuccessCriteria`: Performance thresholds (PerformanceCriteria)

**Business Rules**:
- Duration must be between 10 seconds and 10 minutes for educational use
- Load patterns must be realistic for learning environment
- Success criteria aligned with educational performance targets

### LoadPatternConfig
Configuration for k6 load generation patterns.

**Properties**:
- `PatternType`: Load pattern type (enum: Constant, Ramp, Spike, Stress)
- `VirtualUsers`: Concurrent user simulation count (int)
- `RequestsPerSecond`: Target throughput (int?)
- `RampUpDuration`: Gradual load increase period (TimeSpan?)
- `SustainDuration`: Steady load maintenance period (TimeSpan)
- `RampDownDuration`: Gradual load decrease period (TimeSpan?)

### PerformanceCriteria
Success thresholds for test scenario validation.

**Properties**:
- `MaxResponseTime`: Maximum acceptable response time (TimeSpan)
- `MaxErrorRate`: Maximum acceptable error percentage (double)
- `MinThroughput`: Minimum required throughput (int)
- `MaxMemoryUsage`: Maximum memory consumption (long bytes)
- `MaxCpuUsage`: Maximum CPU utilization percentage (double)

### PerformanceMetrics
Collected data points about system behavior during testing.

**Properties**:
- `MetricId`: Unique metric record identifier (Guid)
- `ScenarioId`: Associated test scenario (Guid)
- `Timestamp`: Measurement timestamp (DateTimeOffset)
- `ResponseTime`: Request/message processing time (TimeSpan)
- `Throughput`: Messages or requests per second (double)
- `ErrorRate`: Failure percentage (double)
- `ActiveConnections`: Current connection count (int)
- `MemoryUsage`: Current memory consumption (long bytes)
- `CpuUsage`: Current CPU utilization percentage (double)

## Entity Relationships

```
Message (base)
├── AmqpMessage (AMQP-specific routing)
└── MqttMessage (MQTT-specific QoS)

ConnectionInfo (1) ←→ (many) Message (protocol context)

TestScenario (1) ←→ (1) LoadPatternConfig (load generation)
TestScenario (1) ←→ (1) PerformanceCriteria (success thresholds)
TestScenario (1) ←→ (many) PerformanceMetrics (collected results)
```

## Educational Value

Each entity demonstrates key messaging concepts:
- **Message hierarchy**: Protocol abstraction and specialization
- **ConnectionInfo**: Connection lifecycle and fault tolerance
- **TestScenario**: Performance validation methodology
- **Metrics collection**: Observability and system monitoring

The data model supports progressive learning from basic message sending to advanced performance analysis and optimization techniques.
