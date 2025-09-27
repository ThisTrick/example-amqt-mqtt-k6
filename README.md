
<p align="center">
   <img src="https://raw.githubusercontent.com/ThisTrick/example-amqt-mqtt-k6/master/docs/logo-messaging.png" alt="Messaging Platform Logo" width="180"/>
</p>

<h1 align="center">Example AMQP/MQTT Messaging Platform 🚀</h1>

<p align="center">
   <b>Масштабований .NET 8 LTS стек для сучасних меседжинг-платформ з RabbitMQ, MQTT, API, Grafana та k6</b>
</p>

<p align="center">
   <a href="https://github.com/ThisTrick/example-amqt-mqtt-k6/actions"><img src="https://github.com/ThisTrick/example-amqt-mqtt-k6/workflows/.NET%20CI/badge.svg" alt="CI Status"></a>
   <a href="https://github.com/ThisTrick/example-amqt-mqtt-k6/blob/master/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="MIT License"></a>
   <img src="https://img.shields.io/badge/dotnet-8.0-blueviolet" alt=".NET 8.0">
   <img src="https://img.shields.io/badge/RabbitMQ-AMQP-orange" alt="RabbitMQ">
   <img src="https://img.shields.io/badge/MQTTnet-MQTT-blue" alt="MQTTnet">
</p>

---

> **🔥 Відкрий для себе сучасний підхід до меседжинг-платформ!**

Цей репозиторій — твій стартовий майданчик для побудови високонавантажених систем обміну повідомленнями на базі RabbitMQ (AMQP) та MQTT (MQTTnet) з API, логуванням, моніторингом і performance-тестами.


## 🧩 Основні фічі

- ⚡️ **AMQP (RabbitMQ) та MQTT (MQTTnet) консюмери** — готові до production
- 🌐 **API на ASP.NET Core** — REST для публікації та моніторингу
- 📊 **Моніторинг з Grafana** — дашборди для метрик і логів
- 🧪 **Performance-тести з k6** — сценарії для AMQP, MQTT, API
- 📝 **Контрактні, інтеграційні та юніт-тести**
- 🛠 **Docker-інфраструктура** — швидкий старт для розробки та тестування
- 🇺🇦 **Документація українською**

## 📂 Cтруктура проекту

```text
src/      — основний код (консюмери, API, shared)
tests/    — контрактні, інтеграційні, юніт-тести
k6-tests/ — performance-тести (k6)
docker/   — docker-compose, Grafana
docs/     — документація (UA)
specs/    — специфікації, плани, контракти
```


## 🛠️ Технології

- [.NET 8 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (C#)
- [RabbitMQ.Client](https://www.rabbitmq.com/dotnet.html) (AMQP)
- [MQTTnet](https://github.com/dotnet/MQTTnet) (MQTT)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core)
- [Serilog](https://serilog.net/)
- [k6](https://k6.io/)


## 🚀 Швидкий старт

```bash
# 1. Клонувати репозиторій
git clone https://github.com/ThisTrick/example-amqt-mqtt-k6.git
cd example-amqt-mqtt-k6

# 2. Запустити інфраструктуру
docker compose up -d

# 3. Зібрати та запустити проекти
dotnet build ExampleMessaging.sln
dotnet run --project src/ExampleMessaging.Publisher.Api
# або інші проекти з src/

# 4. Performance-тести
k6 run k6-tests/amqp-performance.js
k6 run k6-tests/mqtt-performance.js
k6 run k6-tests/api-load-test.js
```

## 📬 Приклад API-запиту

```http
POST /publish/amqp HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "exchange": "demo",
  "routingKey": "test.key",
  "payload": "Hello, RabbitMQ!"
}
```

## 📊 Grafana Dashboard

<p align="center">
  <img src="https://raw.githubusercontent.com/ThisTrick/example-amqt-mqtt-k6/master/docker/grafana/dashboards/messaging-platform-preview.png" alt="Grafana Dashboard Preview" width="600"/>
</p>


## 📚 Документація

- [`docs/`](docs/) — концепції AMQP, MQTT, performance-тестування (українською)
- [`specs/`](specs/) — специфікації, плани, контракти API
- [AMQP Concepts (UA)](docs/amqp-concepts.ua.md)
- [MQTT Concepts (UA)](docs/mqtt-concepts.ua.md)
- [Performance Testing (UA)](docs/performance-testing.ua.md)


## 🤝 Контакти та внесок

- Автор: [ThisTrick](https://github.com/ThisTrick)
- Ліцензія: MIT
- Pull requests та issues вітаються!

---

<p align="center">
   <b>Зроблено з ❤️ для .NET-спільноти України</b>
</p>
