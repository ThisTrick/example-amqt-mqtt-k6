# Концепції MQTT - Message Queuing Telemetry Transport

## Що таке MQTT?

**Message Queuing Telemetry Transport (MQTT)** - це легкий протокол обміну повідомленнями, спеціально розроблений для пристроїв з обмеженими ресурсами та мереж з низькою пропускною здатністю. MQTT використовує модель publish-subscribe для ефективного обміну даними.

## Основні характеристики

### 🚀 Легкість
- Мінімальні заголовки протоколу (2 байти)
- Низьке споживання батареї
- Ефективне використання мережі

### 🔄 Publish-Subscribe модель
- Відправники (publishers) публікують повідомлення в топіки
- Отримувачі (subscribers) підписуються на цікаві топіки
- Брокер забезпечує доставку повідомлень

### 📡 Ідеально для IoT
- Підтримка нестабільних з'єднань
- Автоматичне перепідключення
- Мінімальні системні вимоги

## Архітектура MQTT

### 1. MQTT Брокер
- Центральний сервер для маршрутизації повідомлень
- Популярні брокери: Eclipse Mosquitto, HiveMQ, EMQ X
- У нашому проекті використовується RabbitMQ з MQTT плагіном

### 2. MQTT Клієнти
- **Publishers** - відправляють повідомлення в топіки
- **Subscribers** - отримують повідомлення з топіків
- Один клієнт може бути одночасно publisher і subscriber

### 3. Топіки (Topics)
- Ієрархічні рядки для категоризації повідомлень
- Приклад: `home/living-room/temperature`
- Використовують роздільник `/` для створення ієрархії

## Рівні якості обслуговування (QoS)

### QoS 0 - At Most Once (Максимум один раз)
- **"Fire and forget"** - відправити і забути
- Найшвидший, але без гарантій доставки
- Використання: неважливі дані, що часто оновлюються

```csharp
var message = new MqttMessage("25.3°C", "sensors/temp", QosLevel.AtMostOnce);
```

### QoS 1 - At Least Once (Мінімум один раз)
- Гарантована доставка з можливими дублікатами
- Вимагає підтвердження (ACK)
- Використання: важливі дані, де дублікати не критичні

```csharp
var message = new MqttMessage("Alert!", "alarms/fire", QosLevel.AtLeastOnce);
```

### QoS 2 - Exactly Once (Точно один раз)
- Гарантована доставка без дублікатів
- Найповільніший через 4-step handshake
- Використання: критично важливі дані

```csharp
var message = new MqttMessage("Payment: $100", "payments/transaction", QosLevel.ExactlyOnce);
```

## Структура топіків

### Правила іменування
```
level1/level2/level3/...
```

### Приклади добрих топіків:
- `home/bedroom/humidity`
- `factory/line1/machine3/status`
- `vehicle/truck001/gps/coordinates`
- `sensors/building-a/floor-2/room-201/temperature`

### Спеціальні символи:
- **`#`** - багаторівневий wildcard
- **`+`** - однорівневий wildcard
- **`$`** - системні топіки (наприклад, `$SYS/broker/uptime`)

### Wildcard приклади:
```csharp
// Підписка на всі сенсори температури
client.Subscribe("sensors/+/temperature");

// Підписка на всі повідомлення з будинку
client.Subscribe("home/#");

// Підписка на статус всіх машин на лінії 1
client.Subscribe("factory/line1/+/status");
```

## Retained Messages (Збережені повідомлення)

### Що це?
- Останнє повідомлення зберігається брокером
- Нові підписники одразу отримують останнє значення
- Корисно для статусних повідомлень

```csharp
var status = new MqttMessage("online", "devices/sensor001/status", QosLevel.AtLeastOnce)
{
    Retain = true
};
```

### Використання:
- Статус пристроїв (online/offline)
- Поточні показники сенсорів
- Конфігураційні дані

## Last Will and Testament (Заповіт)

### Концепція
- Повідомлення, яке відправляється при несподіваному відключенні клієнта
- Налаштовується при підключенні
- Використовується для сповіщення про збої

```csharp
var lastWill = new MqttMessage("offline", "devices/sensor001/status", QosLevel.AtLeastOnce)
{
    Retain = true
};
```

## Практичні приклади використання

### 1. Система розумного дому
```csharp
// Публікація температури
await publisher.PublishAsync(
    new MqttMessage("22.5", "home/living-room/temperature", QosLevel.AtMostOnce));

// Публікація команди
await publisher.PublishAsync(
    new MqttMessage("turn-on", "home/living-room/light/command", QosLevel.AtLeastOnce));
```

### 2. IoT сенсори
```csharp
// Дані з датчика вологості
var humidityData = new MqttMessage(
    $"{{\"humidity\": {humidity}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}",
    "sensors/greenhouse/zone1/humidity",
    QosLevel.AtLeastOnce
);
```

### 3. Промислові системи
```csharp
// Статус обладнання
var machineStatus = new MqttMessage(
    "running",
    "factory/production-line/machine-5/status",
    QosLevel.AtLeastOnce
)
{
    Retain = true // Зберегти статус для нових підписників
};
```

## Налаштування в проекті

### 1. Конфігурація MQTT
```json
{
  "MQTT": {
    "Server": "localhost",
    "Port": 1883,
    "ClientId": "ExampleMessaging.Publisher",
    "UserName": "",
    "Password": "",
    "KeepAlivePeriodSeconds": 60,
    "CleanSession": true,
    "ConnectTimeoutSeconds": 30,
    "TopicPrefix": "messaging/"
  }
}
```

### 2. Створення MQTT повідомлення
```csharp
var sensorData = new MqttMessage(
    payload: "{\"temperature\": 23.5, \"unit\": \"celsius\"}",
    topic: "sensors/room1/temperature",
    qosLevel: QosLevel.AtLeastOnce,
    messageType: MessageType.Event,
    source: "temperature-sensor-001"
)
{
    Retain = false,
    DupFlag = false
};
```

### 3. Валідація повідомлення
```csharp
if (message.IsValid())
{
    // Перевіряє топік на відсутність wildcards для publish
    // Перевіряє розмір payload
    await mqttService.PublishAsync(message);
}
```

## Порівняння MQTT vs інші протоколи

| Критерій | MQTT | AMQP | HTTP | CoAP |
|----------|------|------|------|------|
| **Розмір заголовків** | 2+ байти | ~8 байтів | 100+ байтів | 4+ байти |
| **QoS рівні** | 3 (0,1,2) | Складніше | Немає | 2 (0,1) |
| **Транспорт** | TCP | TCP | TCP | UDP |
| **Складність** | Проста | Висока | Середня | Низька |
| **IoT придатність** | Відмінна | Добра | Погана | Відмінна |

## Сценарії використання MQTT

### ✅ Ідеально для:
- **IoT пристрої** (сенсори, актуатори)
- **Мобільні додатки** (push notifications)
- **Телеметрія** (збір даних з пристроїв)
- **Чат додатки** (легкі повідомлення)
- **Системи моніторингу** (статуси пристроїв)

### ❌ Не підходить для:
- Великі файли або медіа
- Складна маршрутизація повідомлень
- Транзакційні операції
- Критично важливі фінансові операції

## Безпека MQTT

### 1. Аутентифікація
```csharp
var connectionInfo = new ConnectionInfo(ProtocolType.Mqtt, "secure-broker.com", 8883)
{
    Username = "device001",
    Password = "secure-password",
    IsSecure = true // TLS/SSL
};
```

### 2. Авторизація
- ACL (Access Control Lists) на рівні брокера
- Контроль доступу до топіків
- Різні права для read/write

### 3. Шифрування
- **MQTT over TLS** (порт 8883)
- **MQTT over WebSockets + TLS** (порт 443)
- Сертифікати для взаємної аутентифікації

## Моніторинг MQTT

### Ключові метрики:
- Кількість підключених клієнтів
- Швидкість публікації повідомлень
- Розмір черг retained повідомлень
- Кількість активних підписок

### Інструменти:
- **MQTT Explorer** - GUI клієнт для тестування
- **Grafana** - візуалізація метрик
- **k6** - навантажувальне тестування

## Рекомендації з продуктивності

### 1. Оптимізація топіків
```csharp
// ✅ Добре - структуровані топіки
"sensors/building1/floor2/room201/temperature"

// ❌ Погано - занадто глибока ієрархія
"company/department/team/project/module/component/sensor/data"
```

### 2. Вибір QoS
- **QoS 0** для частих, неважливих даних
- **QoS 1** для більшості випадків
- **QoS 2** тільки коли критично важливо

### 3. Retained Messages
- Використовуйте для статусних повідомлень
- Очищайте непотрібні retained повідомлення
- Моніторте кількість retained повідомлень

## Приклади з проекту

У цьому навчальному проекті MQTT використовується для:
- **Публікації IoT даних** через REST API
- **Демонстрації різних QoS рівнів**
- **Тестування retained повідомлень**
- **Навантажувального тестування** з різними QoS

Дивіться реалізацію в:
- `src/ExampleMessaging.Mqtt.Consumer/`
- `k6-tests/mqtt-performance.js`
- Grafana dashboard для MQTT метрик
