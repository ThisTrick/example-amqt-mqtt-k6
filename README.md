# Example AMQP/MQTT Messaging Platform

Цей репозиторій містить приклади використання AMQP (RabbitMQ) та MQTT (MQTTnet) у .NET 8 LTS, а також API на ASP.NET Core, логування через Serilog і performance-тести з k6.

## Структура проекту

- `src/` — основний код (консюмери AMQP/MQTT, API, спільні бібліотеки)
- `tests/` — контрактні, інтеграційні та юніт-тести
- `k6-tests/` — performance-тести для AMQP, MQTT та API
- `docker/` — docker-compose, Grafana dashboards
- `docs/` — документація (UA)
- `specs/` — специфікації, плани, дослідження

## Технології
- .NET 8 LTS (C#)
- RabbitMQ.Client (AMQP)
- MQTTnet (MQTT)
- ASP.NET Core
- Serilog
- k6

## Швидкий старт

1. Клонувати репозиторій:
   ```bash
   git clone https://github.com/ThisTrick/example-amqt-mqtt-k6.git
   cd example-amqt-mqtt-k6
   ```
2. Запустити docker-compose:
   ```bash
   docker compose up -d
   ```
3. Зібрати та запустити проекти:
   ```bash
   dotnet build ExampleMessaging.sln
   dotnet run --project src/ExampleMessaging.Publisher.Api
   # або інші проекти з src/
   ```
4. Запустити performance-тести:
   ```bash
   k6 run k6-tests/amqp-performance.js
   k6 run k6-tests/mqtt-performance.js
   k6 run k6-tests/api-load-test.js
   ```

## Документація
- `docs/` — концепції AMQP, MQTT, performance-тестування (українською)
- `specs/` — специфікації, плани, контракти API

## Контакти
- Автор: [ThisTrick](https://github.com/ThisTrick)
- Ліцензія: MIT
