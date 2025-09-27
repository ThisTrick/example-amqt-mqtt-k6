# 📨 Example AMQP/MQTT k6 - Навчальна платформа обміну повідомленнями

> Комплексний навчальний проект для вивчення сучасних технологій обміну повідомленнями з використанням .NET 8, AMQP, MQTT та k6 performance testing.

## 🎯 Огляд проекту

Цей проект демонструє повну реалізацію messaging платформи з використанням найсучасніших технологій. Він створений як навчальний ресурс для розробників, які хочуть освоїти:

- **Message Brokers** - RabbitMQ для AMQP та MQTT протоколів
- **Microservices Architecture** - розподілена архітектура з .NET 8
- **Performance Testing** - навантажувальне тестування з k6
- **Containerization** - Docker та Docker Compose
- **Monitoring** - Grafana та Prometheus метрики

## 🏗️ Архітектура системи

```mermaid
graph TB
    subgraph "Client Applications"
        API[REST API Client]
        WEB[Web Dashboard]
        MOBILE[Mobile App]
    end
    
    subgraph "API Gateway Layer"
        GATE[API Gateway]
    end
    
    subgraph "Services Layer"
        PUB[Publisher Service]
        AMQP_CON[AMQP Consumer]
        MQTT_CON[MQTT Consumer]
    end
    
    subgraph "Message Brokers"
        RABBIT[RabbitMQ<br/>AMQP + MQTT]
    end
    
    subgraph "Monitoring"
        GRAF[Grafana]
        PROM[Prometheus]
        K6[k6 Testing]
    end
    
    API --> GATE
    WEB --> GATE
    MOBILE --> GATE
    
    GATE --> PUB
    PUB --> RABBIT
    RABBIT --> AMQP_CON
    RABBIT --> MQTT_CON
    
    PUB -.-> PROM
    AMQP_CON -.-> PROM
    MQTT_CON -.-> PROM
    PROM --> GRAF
    
    K6 --> GATE
```

## 🚀 Швидкий старт

### Передумови

- **Docker & Docker Compose** - для запуску інфраструктури
- **.NET 8 SDK** - для розробки та запуску сервісів
- **k6** - для тестування продуктивності (опціонально)

### 1. Клонування репозиторію

```bash
git clone https://github.com/your-username/example-amqt-mqtt-k6.git
cd example-amqt-mqtt-k6
```

### 2. Запуск інфраструктури

```bash
# Запуск RabbitMQ, Grafana, Prometheus
docker-compose up -d

# Перевірка статусу
docker-compose ps
```

### 3. Збірка та запуск сервісів

```bash
# Збірка всіх проектів
dotnet build

# Запуск Publisher API
cd src/ExampleMessaging.Publisher.Api
dotnet run

# В іншому терміналі - AMQP Consumer
cd src/ExampleMessaging.Amqp.Consumer
dotnet run

# В третьому терміналі - MQTT Consumer
cd src/ExampleMessaging.Mqtt.Consumer
dotnet run
```

### 4. Перевірка роботи

```bash
# Надіслати AMQP повідомлення
curl -X POST http://localhost:5000/api/amqp/publish \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Hello AMQP!",
    "routingKey": "test.message",
    "exchange": "direct-exchange"
  }'

# Надіслати MQTT повідомлення
curl -X POST http://localhost:5000/api/mqtt/publish \
  -H "Content-Type: application/json" \
  -d '{
    "payload": "Hello MQTT!",
    "topic": "test/message",
    "qosLevel": "AtLeastOnce"
  }'
```

## 📚 Документація

### Концептуальні керівництва:
- 📖 [**AMQP Концепції**](./amqp-concepts.ua.md) - Advanced Message Queuing Protocol
- 📱 [**MQTT Концепції**](./mqtt-concepts.ua.md) - Message Queuing Telemetry Transport
- ⚡ [**Performance Testing**](./performance-testing.ua.md) - k6 навантажувальне тестування

### Технічна документація:
- 🔧 [**API Reference**](../src/ExampleMessaging.Publisher.Api/README.md) - REST API endpoints
- 🏗️ [**Architecture Guide**](./architecture.md) - Детальна архітектура системи
- 🐳 [**Deployment Guide**](./deployment.md) - Production deployment

## 🛠️ Структура проекту

```
example-amqt-mqtt-k6/
├── 📁 src/                           # Вихідний код
│   ├── ExampleMessaging.Shared/       # Загальні моделі та утиліти
│   ├── ExampleMessaging.Publisher.Api/ # REST API для публікації
│   ├── ExampleMessaging.Amqp.Consumer/ # AMQP споживач
│   └── ExampleMessaging.Mqtt.Consumer/ # MQTT споживач
├── 📁 tests/                         # Unit та інтеграційні тести
│   └── ExampleMessaging.UnitTests/    # Unit тести (76 тестів)
├── 📁 k6-tests/                      # Performance тести
│   ├── api-performance.js            # REST API тести
│   ├── amqp-performance.js           # AMQP тести
│   └── mqtt-performance.js           # MQTT тести
├── 📁 docs/                          # Документація
│   ├── amqp-concepts.ua.md           # AMQP керівництво
│   ├── mqtt-concepts.ua.md           # MQTT керівництво
│   └── performance-testing.ua.md     # k6 керівництво
├── 📁 grafana/                       # Grafana dashboards
├── 📁 prometheus/                    # Prometheus конфігурація
└── docker-compose.yml               # Інфраструктура
```

## 🔧 Компоненти системи

### 1. Publisher API Service
- **Порт:** 5000 (HTTP), 5001 (HTTPS)
- **Функції:** REST API для публікації повідомлень
- **Технології:** ASP.NET Core 8, Swagger/OpenAPI
- **Endpoints:**
  - `POST /api/amqp/publish` - AMQP публікація
  - `POST /api/mqtt/publish` - MQTT публікація
  - `GET /health` - Health check

### 2. AMQP Consumer Service
- **Функції:** Обробка AMQP повідомлень з RabbitMQ
- **Features:**
  - Підтримка різних exchange типів
  - Dead letter queues
  - Message acknowledgments
  - Retry policies

### 3. MQTT Consumer Service
- **Функції:** Обробка MQTT повідомлень
- **Features:**
  - QoS рівні 0, 1, 2
  - Wildcard subscriptions
  - Retained messages
  - Last Will and Testament

### 4. Shared Library
- **Моделі:** `AmqpMessage`, `MqttMessage`, `ConnectionInfo`
- **Утиліти:** Валідація, серіалізація, логування
- **Enums:** `QosLevel`, `MessageType`, `ProtocolType`

## 📊 Моніторинг та метрики

### Grafana Dashboards (http://localhost:3000)
- **System Overview** - загальна продуктивність
- **AMQP Metrics** - метрики AMQP трафіку
- **MQTT Metrics** - метрики MQTT трафіку
- **Application Metrics** - .NET додатки

### Prometheus Metrics (http://localhost:9090)
- `amqp_messages_published_total` - опубліковані AMQP повідомлення
- `mqtt_messages_published_total` - опубліковані MQTT повідомлення
- `message_processing_duration_seconds` - час обробки
- `active_connections_count` - активні з'єднання

### RabbitMQ Management (http://localhost:15672)
- **Login:** guest / guest
- Управління queue, exchange, bindings
- Моніторинг з'єднань та каналів

## 🧪 Тестування

### Unit Tests
```bash
# Запуск всіх unit тестів (76 тестів)
dotnet test tests/ExampleMessaging.UnitTests/

# Тести з покриттям коду
dotnet test --collect:"XPlat Code Coverage"
```

### Performance Tests
```bash
# Smoke test - швидка перевірка
k6 run k6-tests/smoke-test.js

# Load test - стандартне навантаження
k6 run k6-tests/api-performance.js

# Stress test - пікове навантаження
k6 run k6-tests/stress-test.js
```

### Інтеграційні тести
```bash
# Запуск інтеграційних тестів
dotnet test tests/ExampleMessaging.IntegrationTests/
```

## 🎓 Навчальні сценарії

### Сценарій 1: Вивчення AMQP
1. Прочитайте [AMQP Концепції](./amqp-concepts.ua.md)
2. Експериментуйте з різними exchange типами
3. Тестуйте routing patterns
4. Вивчайте dead letter queues

### Сценарій 2: Освоєння MQTT
1. Прочитайте [MQTT Концепції](./mqtt-concepts.ua.md)
2. Тестуйте різні QoS рівні
3. Експериментуйте з wildcard subscriptions
4. Налаштуйте retained messages

### Сценарій 3: Performance Testing
1. Прочитайте [Performance Testing](./performance-testing.ua.md)
2. Запустіть базові k6 тести
3. Аналізуйте метрики в Grafana
4. Оптимізуйте конфігурацію

### Сценарій 4: Monitoring Setup
1. Налаштуйте custom метрики
2. Створіть власні Grafana dashboards
3. Налаштуйте alerting rules
4. Інтегруйте з notification channels

## 🔍 Приклади використання

### AMQP Direct Exchange
```csharp
var message = new AmqpMessage(
    content: "Hello Direct Exchange!",
    routingKey: "user.created",
    exchange: "user-events",
    messageType: MessageType.Event
)
{
    Priority = 5,
    Headers = new Dictionary<string, object>
    {
        ["userId"] = "12345",
        ["timestamp"] = DateTime.UtcNow
    }
};

await amqpPublisher.PublishAsync(message);
```

### MQTT Topic Subscription
```csharp
var message = new MqttMessage(
    payload: "{\"temperature\": 23.5, \"humidity\": 65}",
    topic: "sensors/living-room/climate",
    qosLevel: QosLevel.AtLeastOnce
)
{
    Retain = true // Зберегти для нових підписників
};

await mqttPublisher.PublishAsync(message);
```

### k6 Performance Test
```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },
    { duration: '5m', target: 100 },
    { duration: '2m', target: 0 },
  ],
};

export default function () {
  const response = http.post('http://localhost:5000/api/amqp/publish', 
    JSON.stringify({
      content: `Message ${__ITER}`,
      routingKey: 'test.performance',
      exchange: 'perf-test'
    }), {
      headers: { 'Content-Type': 'application/json' },
    }
  );

  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

## 🔧 Налаштування середовища

### Development Environment
```json
{
  "AMQP": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "Exchange": "dev-exchange",
    "RetryCount": 3
  },
  "MQTT": {
    "Server": "localhost",
    "Port": 1883,
    "ClientId": "dev-client",
    "KeepAlivePeriodSeconds": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Production Environment
```json
{
  "AMQP": {
    "ConnectionString": "amqps://user:pass@prod-rabbitmq:5671/",
    "Exchange": "prod-exchange",
    "RetryCount": 5,
    "Timeout": 30
  },
  "MQTT": {
    "Server": "prod-mqtt.company.com",
    "Port": 8883,
    "ClientId": "prod-publisher-001",
    "IsSecure": true
  }
}
```

## 🚢 Deployment

### Docker Production Deployment
```bash
# Збірка production images
docker build -t messaging-publisher-api src/ExampleMessaging.Publisher.Api/
docker build -t messaging-amqp-consumer src/ExampleMessaging.Amqp.Consumer/
docker build -t messaging-mqtt-consumer src/ExampleMessaging.Mqtt.Consumer/

# Запуск production stack
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: messaging-publisher-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: messaging-publisher-api
  template:
    metadata:
      labels:
        app: messaging-publisher-api
    spec:
      containers:
      - name: api
        image: messaging-publisher-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## 🤝 Співпраця

### Як долучитися:
1. Fork проект
2. Створіть feature branch (`git checkout -b feature/amazing-feature`)
3. Commit зміни (`git commit -m 'Add some amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Відкрийте Pull Request

### Coding Standards:
- Використовуйте C# naming conventions
- Покривайте код unit тестами
- Додавайте XML documentation
- Слідуйте SOLID принципам

## 📈 Roadmap

### Phase 1: Core Messaging ✅
- [x] AMQP Producer/Consumer
- [x] MQTT Publisher/Subscriber
- [x] REST API interfaces
- [x] Basic monitoring

### Phase 2: Advanced Features ✅
- [x] Performance testing with k6
- [x] Grafana dashboards
- [x] Comprehensive unit tests
- [x] Ukrainian documentation

### Phase 3: Enterprise Features 🚀
- [ ] Authentication & Authorization
- [ ] Message persistence
- [ ] Cluster deployment
- [ ] Advanced monitoring

### Phase 4: Extensions 📋
- [ ] WebSocket support
- [ ] GraphQL API
- [ ] Message transformation
- [ ] Event sourcing patterns

## 📄 Ліцензія

Цей проект ліцензовано під MIT License - дивіться [LICENSE](LICENSE) файл для деталей.

## 🙏 Acknowledgments

- **RabbitMQ Team** за чудовий message broker
- **k6 Community** за потужний інструмент тестування
- **Grafana Labs** за моніторинг та візуалізацію
- **.NET Community** за відмінну ecosystem

## 📞 Контакти та підтримка

- 📧 **Email:** support@example-messaging.com
- 💬 **Discord:** [Example Messaging Community](https://discord.gg/example)
- 📖 **Wiki:** [GitHub Wiki](https://github.com/your-username/example-amqt-mqtt-k6/wiki)
- 🐛 **Issues:** [GitHub Issues](https://github.com/your-username/example-amqt-mqtt-k6/issues)

---

**⭐ Якщо цей проект виявився корисним, поставте зірочку!**

*Останнє оновлення: Грудень 2024*
