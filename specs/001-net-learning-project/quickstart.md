# Quickstart: .NET Messaging Learning Platform

## Швидкий старт навчальної платформи обміну повідомленнями

### Передумови

**Необхідне програмне забезпечення**:
- Docker та Docker Compose
- .NET 8 SDK
- Git для клонування репозиторію

**Перевірка готовності**:
```bash
docker --version          # >= 20.10
docker-compose --version  # >= 2.0
dotnet --version          # >= 8.0
```

### 1. Запуск повного середовища

**Клонування та підготовка**:
```bash
git clone <repository-url>
cd example-amqt-mqtt-k6
git checkout 001-net-learning-project
```

**Запуск всіх сервісів**:
```bash
# Запуск брокера повідомлень та сервісів моніторингу
docker-compose up -d rabbitmq grafana

# Очікування готовності брокера (30 секунд)
sleep 30

# Збірка та запуск .NET сервісів
dotnet build ExampleMessaging.sln
dotnet run --project src/ExampleMessaging.Publisher.Api &
dotnet run --project src/ExampleMessaging.Amqp.Consumer &
dotnet run --project src/ExampleMessaging.Mqtt.Consumer &
```

**Перевірка готовності**:
```bash
# Перевірка здоров'я API
curl http://localhost:5000/health

# Перевірка RabbitMQ Management UI
open http://localhost:15672  # admin/admin

# Перевірка Grafana
open http://localhost:3000   # admin/admin
```

### 2. Тестування AMQP (Advanced Message Queuing Protocol)

**Публікація повідомлення через AMQP**:
```bash
curl -X POST http://localhost:5000/amqp/publish \
  -H "Content-Type: application/json" \
  -d '{
    "payload": "Привіт, AMQP світ!",
    "exchange": "learning.direct", 
    "routingKey": "orders.created",
    "messageType": "Event",
    "persistent": true
  }'
```

**Очікуваний результат**:
- HTTP 202 Accepted response з messageId
- Повідомлення з'являється в RabbitMQ Management UI
- AMQP Consumer обробляє повідомлення (перевірити логи)

**Розуміння AMQP**:
- **Exchange**: Маршрутизатор повідомлень (приймає та направляє)
- **Queue**: Зберігає повідомлення для споживачів
- **Routing Key**: Критерій маршрутизації для direct exchange
- **Persistent**: Повідомлення вижили б перезапуск брокера

### 3. Тестування MQTT (Message Queuing Telemetry Transport)

**Публікація повідомлення через MQTT**:
```bash
curl -X POST http://localhost:5000/mqtt/publish \
  -H "Content-Type: application/json" \
  -d '{
    "payload": "Привіт, MQTT світ!",
    "topic": "sensors/temperature/room1",
    "qosLevel": 1,
    "retain": false,
    "messageType": "Event"
  }'
```

**Очікуваний результат**:
- HTTP 202 Accepted response з messageId
- MQTT Consumer отримує повідомлення на topic
- Повідомлення відображається в логах з QoS підтвердженням

**Розуміння MQTT**:
- **Topic**: Ієрархічний шлях для категоризації повідомлень
- **QoS Level 1**: "At least once" - гарантія доставки з можливими дублікатами
- **Retain**: Брокер зберігає останнє повідомлення для нових підписників
- **Subscribe**: Споживачі підписуються на топіки (може містити wildcards)

### 4. Тестування продуктивності з k6

**Базове навантажувальне тестування API**:
```bash
# Запуск k6 тесту API
k6 run k6-tests/api-load-test.js

# Очікуваний результат:
# - 100 req/s протягом 30 секунд
# - < 100ms середній час відповіді
# - < 1% помилок
```

**Тестування продуктивності AMQP**:
```bash
# Запуск специфічного тесту AMQP
k6 run k6-tests/amqp-performance.js

# Очікуваний результат:
# - 1000 msg/s публікація
# - Все повідомлення оброблені споживачем
# - Візуальні метрики в Grafana
```

**Тестування продуктивності MQTT**:
```bash
# Запуск специфічного тесту MQTT  
k6 run k6-tests/mqtt-performance.js

# Очікуваний результат:
# - 1000 msg/s публікація
# - QoS рівні дотримані
# - Топіки правильно маршрутизовані
```

### 5. Моніторинг та візуалізація

**Перегляд метрик у Grafana**:
1. Відкрити http://localhost:3000
2. Логін: admin / admin
3. Перейти до Dashboard: "Messaging Learning Platform"
4. Переглянути панелі:
   - HTTP API Metrics (запити/с, час відповіді, помилки)
   - AMQP Metrics (повідомлення/с, з'єднання, черги)
   - MQTT Metrics (повідомлення/с, підписки, топіки)
   - System Metrics (CPU, пам'ять, диск)

**Аналіз логів**:
```bash
# Логи Publisher API
docker logs messaging-platform-api

# Логи AMQP Consumer  
docker logs messaging-platform-amqp-consumer

# Логи MQTT Consumer
docker logs messaging-platform-mqtt-consumer

# Логи RabbitMQ
docker logs messaging-platform-rabbitmq
```

### 6. Експерименти для навчання

**Сценарій 1: Відмова брокера**:
```bash
# Зупинити RabbitMQ
docker stop messaging-platform-rabbitmq

# Спробувати публікацію (має повернути 503)
curl -X POST http://localhost:5000/amqp/publish -d '...'

# Перезапустити та перевірити відновлення
docker start messaging-platform-rabbitmq
```

**Сценарій 2: Навантаження споживача**:
```bash
# Зупинити споживача AMQP
docker stop messaging-platform-amqp-consumer

# Публікувати багато повідомлень
for i in {1..100}; do
  curl -X POST http://localhost:5000/amqp/publish -d '{...}'
done

# Запустити споживача - побачити пакетну обробку
docker start messaging-platform-amqp-consumer
```

**Сценарій 3: Порівняння QoS рівнів MQTT**:
```bash
# QoS 0 (At most once)
curl -X POST http://localhost:5000/mqtt/publish -d '{"qosLevel": 0, ...}'

# QoS 1 (At least once) 
curl -X POST http://localhost:5000/mqtt/publish -d '{"qosLevel": 1, ...}'

# QoS 2 (Exactly once)
curl -X POST http://localhost:5000/mqtt/publish -d '{"qosLevel": 2, ...}'

# Порівняти поведінку в логах споживача
```

### 7. Зупинка середовища

**Коректна зупинка всіх сервісів**:
```bash
# Зупинити .NET процеси
pkill -f "dotnet run"

# Зупинити Docker контейнери
docker-compose down

# Очистити ресурси (необов'язково)
docker system prune -f
```

## Результати навчання

Після виконання quickstart ви повинні розуміти:

**AMQP концепції**:
- Різниця між Exchange та Queue
- Роль Routing Key у маршрутизації
- Persistent vs Transient повідомлення
- Acknowledgments та гарантії доставки

**MQTT концепції**:
- Топіки та wildcards у підписках
- QoS рівні та їх вплив на продуктивність
- Retained повідомлення та їх використання
- Client ID та Clean Session поведінка

**Продуктивність**:
- Базові показники швидкості для API та брокерів
- Вплив QoS рівнів на пропускну здатність
- Моніторинг ресурсів системи
- Візуалізація метрик у реальному часі

**Відмовостійкість**:
- Поведінка при недоступності брокера
- Стратегії повторних спроб з'єднання
- Накопичення повідомлень у чергах
- Graceful shutdown та recovery

## Наступні кроки

Після успішного завершення quickstart:
1. Прочитайте детальну документацію у `/docs/`
2. Вивчіть імплементацію у вихідному коді
3. Запустіть unit та integration тести
4. Експериментуйте з різними конфігураціями брокера
5. Спробуйте створити власні сценарії тестування
