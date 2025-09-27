<!--
Sync Impact Report:
Version change: None → 1.0.0 (Initial constitution)
New principles:
- Protocol Learning Focus
- .NET Best Practices
- Test-Driven Development (NON-NEGOTIABLE)
- Docker Containerization
- Performance Observability
Added sections:
- Learning Requirements
- Development Standards
Templates requiring updates: ✅ All templates align with TDD and .NET focus
Follow-up TODOs: None
-->

# Example AMQP MQTT k6 Constitution

## Core Principles

### I. Protocol Learning Focus
Every implementation must demonstrate practical understanding of messaging protocols.
AMQP and MQTT examples must show real-world patterns: connection management, message persistence, QoS levels, topic routing.
k6 tests must validate protocol behavior under load, not just HTTP endpoints.

**Rationale**: This project exists for educational purposes - each component must teach protocol fundamentals.

### II. .NET Best Practices
All .NET code follows modern C# practices: async/await patterns, dependency injection, configuration patterns, structured logging.
Use official .NET messaging libraries: RabbitMQ.Client for AMQP, MQTTnet for MQTT.
Follow Microsoft's coding conventions and project structure guidelines.

**Rationale**: Learning protocols within .NET context requires demonstrating idiomatic .NET development patterns.

### III. Test-Driven Development (NON-NEGOTIABLE)
TDD mandatory: Tests written → User approved → Tests fail → Then implement.
Red-Green-Refactor cycle strictly enforced for all protocol implementations.
Integration tests MUST validate actual protocol communication, not mocks.

**Rationale**: Protocol implementations are complex and error-prone; TDD ensures correctness and provides living documentation.

### IV. Docker Containerization
All services (AMQP broker, MQTT broker, applications) run in Docker containers.
Docker Compose orchestrates the complete learning environment.
No local installation dependencies beyond Docker and .NET SDK.

**Rationale**: Consistent development environment across different machines; easy setup for learning scenarios.

### V. Performance Observability
k6 performance tests for every protocol scenario with meaningful metrics.
Structured logging with correlation IDs across all services.
Grafana dashboards showing protocol-specific metrics (message rates, connection counts, latency percentiles).

**Rationale**: Understanding protocol performance characteristics is essential for production readiness.

## Learning Requirements

**Hands-on Examples**: Each protocol must have producer, consumer, and error handling examples.
**Documentation**: README sections explaining protocol concepts, not just code usage.
**Progressive Complexity**: Start with basic pub/sub, advance to routing, persistence, clustering.
**Real Scenarios**: Examples based on actual use cases (IoT telemetry, microservice messaging, event sourcing).

## Development Standards

**.NET Target**: .NET 8 LTS with latest C# language features.
**Project Structure**: Solution with separate projects for each protocol + shared libraries.
**Configuration**: appsettings.json with environment-specific overrides.
**Error Handling**: Global exception handling with protocol-specific error recovery.
**Logging**: Serilog with structured logging to console and file sinks.

## Governance

Constitution supersedes all other development practices for this learning project.
All implementations must demonstrate compliance with protocol learning focus and .NET best practices.
Amendments require practical validation through working examples before adoption.
TDD principle is non-negotiable and cannot be waived for time constraints.

**Version**: 1.0.0 | **Ratified**: 2025-09-27 | **Last Amended**: 2025-09-27
