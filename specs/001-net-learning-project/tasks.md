# Tasks: .NET Messaging Learning Platform

**Input**: Design documents from `/home/den/git/example-amqt-mqtt-k6/specs/001-net-learning-project/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/ (✓), quickstart.md (✓)

## Execution Flow (main)
```
1. Load plan.md from feature directory ✓
   → Extract: C# .NET 8, RabbitMQ.Client, MQTTnet, ASP.NET Core, xUnit, Testcontainers
2. Load design documents ✓
   → data-model.md: 8 entities → model tasks
   → contracts/: API contract → contract test tasks  
   → research.md: Tech decisions → setup tasks
   → quickstart.md: User scenarios → integration test tasks
3. Generate tasks by category:
   → Setup: .NET solution, Docker, dependencies
   → Tests: contract tests, integration tests (TDD mandatory)
   → Core: models, services, controllers, consumers
   → Integration: RabbitMQ, logging, configuration
   → Polish: unit tests, k6 performance, Ukrainian docs
4. Apply task rules:
   → Different projects = mark [P] for parallel
   → Same project = sequential (no [P])
   → Tests before implementation (TDD constitutional requirement)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph ✓
7. Create parallel execution examples ✓
8. Validate task completeness ✓
   → All API contracts have tests ✓
   → All entities have models ✓  
   → All endpoints implemented ✓
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different projects/files, no dependencies)
- Include exact file paths in descriptions
- Follow TDD: Tests → Fail → Implement → Pass

## Path Conventions
Based on plan.md structure:
- **Solution**: `ExampleMessaging.sln` at repository root
- **Shared**: `src/ExampleMessaging.Shared/`
- **API**: `src/ExampleMessaging.Publisher.Api/`
- **Consumers**: `src/ExampleMessaging.Amqp.Consumer/`, `src/ExampleMessaging.Mqtt.Consumer/`
- **CLI**: `src/ExampleMessaging.Cli/`
- **Tests**: `tests/ExampleMessaging.IntegrationTests/`, `tests/ExampleMessaging.ContractTests/`, `tests/ExampleMessaging.UnitTests/`

## Phase 3.1: Setup
- [x] T001 Create .NET solution structure: `ExampleMessaging.sln` with project references per plan.md
- [x] T002 [P] Initialize ExampleMessaging.Shared class library with models, configuration, extensions folders
- [x] T003 [P] Initialize ExampleMessaging.Publisher.Api ASP.NET Core project with controllers, services folders  
- [x] T004 [P] Initialize ExampleMessaging.Amqp.Consumer console app with handlers, services folders
- [x] T005 [P] Initialize ExampleMessaging.Mqtt.Consumer console app with handlers, services folders
- [x] T006 [P] Initialize ExampleMessaging.Cli console app with commands folder
- [x] T007 [P] Initialize test projects: IntegrationTests, ContractTests, UnitTests with xUnit and Testcontainers
- [x] T008 Add NuGet dependencies: RabbitMQ.Client, MQTTnet, Serilog, Testcontainers.RabbitMq to respective projects
- [x] T009 Create Docker Compose file: `docker/docker-compose.yml` with RabbitMQ, Grafana services per quickstart.md
- [x] T010 [P] Configure EditorConfig and solution-level linting/formatting tools

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### Contract Tests (API Schema Validation)
- [x] T011 [P] Contract test POST /amqp/publish in `tests/ExampleMessaging.ContractTests/AmqpPublishContractTests.cs`
- [x] T012 [P] Contract test POST /mqtt/publish in `tests/ExampleMessaging.ContractTests/MqttPublishContractTests.cs`
- [x] T013 [P] Contract test GET /health in `tests/ExampleMessaging.ContractTests/HealthCheckContractTests.cs`
- [x] T014 [P] Contract test GET /metrics in `tests/ExampleMessaging.ContractTests/MetricsContractTests.cs`

### Integration Tests (Protocol Communication)
- [x] T015 [P] AMQP integration test: publish → consumer receives in `tests/ExampleMessaging.IntegrationTests/AmqpIntegrationTest.cs`
- [x] T016 [P] MQTT integration test: publish → subscriber receives in `tests/ExampleMessaging.IntegrationTests/MqttIntegrationTests.cs`
- [x] T017 [P] End-to-end test: API → AMQP → Consumer processing in `tests/ExampleMessaging.IntegrationTests/EndToEndAmqpTests.cs`
- [x] T018 [P] End-to-end test: API → MQTT → Consumer processing in `tests/ExampleMessaging.IntegrationTests/EndToEndMqttTests.cs`
- [x] T019 [P] Connection recovery test: broker restart scenarios in `tests/ExampleMessaging.IntegrationTests/ConnectionRecoveryTests.cs`
- [x] T020 [P] Performance baseline test: 100 req/s API, 1000 msg/s brokers in `tests/ExampleMessaging.IntegrationTests/PerformanceBaselineTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)

### Shared Models (Base for all projects)
- [x] T021 [P] Message base class in `src/ExampleMessaging.Shared/Models/Message.cs`
- [x] T022 [P] AmqpMessage class in `src/ExampleMessaging.Shared/Models/AmqpMessage.cs` 
- [x] T023 [P] MqttMessage class in `src/ExampleMessaging.Shared/Models/MqttMessage.cs`
- [x] T024 [P] ConnectionInfo class in `src/ExampleMessaging.Shared/Models/ConnectionInfo.cs`
- [x] T025 [P] TestScenario, LoadPatternConfig, PerformanceCriteria classes in `src/ExampleMessaging.Shared/Models/Performance/`
- [x] T026 [P] Enums: MessageType, QosLevel, ConnectionStatus, ProtocolType in `src/ExampleMessaging.Shared/Models/Enums.cs`
- [x] T027 [P] Configuration classes in `src/ExampleMessaging.Shared/Configuration/`

### Publisher API (HTTP Endpoints)
- [x] T028 AmqpController POST /amqp/publish in `src/ExampleMessaging.Publisher.Api/Controllers/AmqpController.cs`
- [x] T029 MqttController POST /mqtt/publish in `src/ExampleMessaging.Publisher.Api/Controllers/MqttController.cs`
- [x] T030 HealthController GET /health in `src/ExampleMessaging.Publisher.Api/Controllers/HealthController.cs`
- [x] T031 MetricsController GET /metrics in `src/ExampleMessaging.Publisher.Api/Controllers/MetricsController.cs`
- [ ] T032 AmqpPublisherService in `src/ExampleMessaging.Publisher.Api/Services/AmqpPublisherService.cs`
- [ ] T033 MqttPublisherService in `src/ExampleMessaging.Publisher.Api/Services/MqttPublisherService.cs`
- [ ] T034 Program.cs with DI, logging, configuration in `src/ExampleMessaging.Publisher.Api/Program.cs`

### AMQP Consumer
- [ ] T035 [P] AmqpConnectionService in `src/ExampleMessaging.Amqp.Consumer/Services/AmqpConnectionService.cs`
- [ ] T036 [P] AmqpMessageHandler in `src/ExampleMessaging.Amqp.Consumer/Handlers/AmqpMessageHandler.cs`
- [ ] T037 [P] Program.cs with hosted service in `src/ExampleMessaging.Amqp.Consumer/Program.cs`

### MQTT Consumer  
- [ ] T038 [P] MqttConnectionService in `src/ExampleMessaging.Mqtt.Consumer/Services/MqttConnectionService.cs`
- [ ] T039 [P] MqttMessageHandler in `src/ExampleMessaging.Mqtt.Consumer/Handlers/MqttMessageHandler.cs`
- [ ] T040 [P] Program.cs with hosted service in `src/ExampleMessaging.Mqtt.Consumer/Program.cs`

### CLI Management Tools
- [ ] T041 [P] StartCommand for environment startup in `src/ExampleMessaging.Cli/Commands/StartCommand.cs`
- [ ] T042 [P] TestCommand for k6 scenario execution in `src/ExampleMessaging.Cli/Commands/TestCommand.cs`
- [ ] T043 [P] StatusCommand for service health monitoring in `src/ExampleMessaging.Cli/Commands/StatusCommand.cs`
- [ ] T044 [P] Program.cs with command parsing in `src/ExampleMessaging.Cli/Program.cs`

## Phase 3.4: Integration & Infrastructure

### Configuration & Logging
- [ ] T045 Serilog structured logging configuration across all projects
- [ ] T046 appsettings.json with RabbitMQ, MQTT connection strings per environment
- [ ] T047 Global exception handling middleware in Publisher API
- [ ] T048 Correlation ID tracking across services

### Docker & Orchestration
- [ ] T049 Dockerfile for Publisher API in `src/ExampleMessaging.Publisher.Api/Dockerfile`
- [ ] T050 [P] Dockerfile for AMQP Consumer in `src/ExampleMessaging.Amqp.Consumer/Dockerfile`
- [ ] T051 [P] Dockerfile for MQTT Consumer in `src/ExampleMessaging.Mqtt.Consumer/Dockerfile`
- [ ] T052 Update docker-compose.yml to include .NET services with proper networking

### Performance Testing
- [ ] T053 [P] k6 API load test script in `k6-tests/api-load-test.js`
- [ ] T054 [P] k6 AMQP performance test script in `k6-tests/amqp-performance.js`  
- [ ] T055 [P] k6 MQTT performance test script in `k6-tests/mqtt-performance.js`
- [ ] T056 Grafana dashboard configuration for messaging metrics

## Phase 3.5: Polish & Documentation

### Unit Tests
- [ ] T057 [P] Unit tests for AmqpMessage validation in `tests/ExampleMessaging.UnitTests/Models/AmqpMessageTests.cs`
- [ ] T058 [P] Unit tests for MqttMessage validation in `tests/ExampleMessaging.UnitTests/Models/MqttMessageTests.cs`
- [ ] T059 [P] Unit tests for ConnectionInfo state transitions in `tests/ExampleMessaging.UnitTests/Models/ConnectionInfoTests.cs`
- [ ] T060 [P] Unit tests for publisher services in `tests/ExampleMessaging.UnitTests/Services/`

### Ukrainian Documentation
- [ ] T061 [P] AMQP concepts guide in `docs/amqp-concepts.ua.md`
- [ ] T062 [P] MQTT concepts guide in `docs/mqtt-concepts.ua.md`
- [ ] T063 [P] Performance testing guide in `docs/performance-testing.ua.md`
- [ ] T064 [P] Main README in `docs/README.ua.md`

### Final Validation
- [ ] T065 Run all integration tests with real RabbitMQ containers
- [ ] T066 Execute quickstart.md validation scenarios
- [ ] T067 Performance benchmarking: meet 100 req/s API, 1000 msg/s broker targets
- [ ] T068 Code cleanup and refactoring for educational clarity

## Dependencies

### Critical Path (Sequential Dependencies)
1. **Setup** (T001-T010) → **Tests** (T011-T020) → **Implementation** (T021-T044)
2. **Shared Models** (T021-T027) must complete before **API/Consumers** (T028-T044)
3. **Tests must FAIL** before implementation begins (constitutional TDD requirement)

### Parallel Execution Groups

**Group A - Test Setup** (After T010):
```bash
# All can run in parallel - different test files
T011, T012, T013, T014, T015, T016, T017, T018, T019, T020
```

**Group B - Shared Models** (After tests written):
```bash
# All can run in parallel - different model files  
T021, T022, T023, T024, T025, T026, T027
```

**Group C - Services** (After models complete):
```bash
# Different projects - can run in parallel
T035, T036, T038, T039, T041, T042, T043
T050, T051, T053, T054, T055
```

**Group D - Documentation** (After implementation):
```bash  
# Different documentation files - fully parallel
T057, T058, T059, T060, T061, T062, T063, T064
```

## Validation Checklist
- ✅ All API contracts have contract tests (T011-T014)
- ✅ All entities have model implementations (T021-T026)  
- ✅ All endpoints have controllers (T028-T031)
- ✅ Both AMQP and MQTT consumers implemented (T035-T040)
- ✅ CLI management tools included (T041-T044)
- ✅ Performance testing with k6 (T053-T055)
- ✅ Ukrainian documentation for learning (T061-T064)
- ✅ TDD workflow: Tests → Fail → Implement → Pass
- ✅ Constitutional compliance: Protocol learning, .NET best practices, Docker containerization

## Task Execution Examples

**Run Contract Tests in Parallel**:
```bash
# After T010 complete - setup all contract tests simultaneously
dotnet test tests/ExampleMessaging.ContractTests/AmqpPublishContractTests.cs &
dotnet test tests/ExampleMessaging.ContractTests/MqttPublishContractTests.cs &
dotnet test tests/ExampleMessaging.ContractTests/HealthCheckContractTests.cs &
dotnet test tests/ExampleMessaging.ContractTests/MetricsContractTests.cs &
wait # All should FAIL - proving tests are written correctly
```

**Build Shared Models in Parallel**:
```bash
# After T020 complete - implement all models simultaneously
# Each model is in different file - no conflicts
code src/ExampleMessaging.Shared/Models/Message.cs &
code src/ExampleMessaging.Shared/Models/AmqpMessage.cs &  
code src/ExampleMessaging.Shared/Models/MqttMessage.cs &
# ... continue for all model files
```

**Deploy Services Together**:
```bash
# After T052 complete - all Dockerfiles ready
docker-compose up --build -d
# All services start together in orchestrated environment
```
