# Керівництво з тестування продуктивності - k6 Performance Testing

## Вступ до k6

**k6** - це сучасний інструмент навантажувального тестування з відкритим кодом, написаний на Go. Він дозволяє створювати реалістичні тести продуктивності використовуючи JavaScript.

### Основні переваги k6:
- 🚀 **Високопродуктивний** - генерує велике навантаження з мінімальними ресурсами
- 📊 **Багаті метрики** - детальна аналітика продуктивності
- 💻 **Developer-friendly** - тести пишуться на JavaScript
- 🔌 **Розширюваний** - підтримує кастомні метрики та плагіни
- ☁️ **Cloud-ready** - легко інтегрується з CI/CD

## Архітектура тестування в проекті

### Структура тестів:
```
k6-tests/
├── api-performance.js      # Тести REST API
├── amqp-performance.js     # Тести AMQP брокера
├── mqtt-performance.js     # Тести MQTT брокера
├── config/
│   ├── development.json    # Налаштування для розробки
│   ├── staging.json        # Налаштування для staging
│   └── production.json     # Налаштування для production
└── utils/
    ├── helpers.js          # Допоміжні функції
    └── scenarios.js        # Різні сценарії навантаження
```

## Типи тестів продуктивності

### 1. Load Testing (Навантажувальне тестування)
**Мета:** Перевірити роботу системи під звичайним навантаженням

```javascript
export let options = {
  stages: [
    { duration: '2m', target: 100 }, // Поступове збільшення до 100 користувачів
    { duration: '5m', target: 100 }, // Тримати 100 користувачів 5 хвилин
    { duration: '2m', target: 0 },   // Поступове зменшення до 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% запитів мають бути < 500ms
    http_req_failed: ['rate<0.1'],    // Менше 10% помилок
  },
};
```

### 2. Stress Testing (Стрес-тестування)
**Мета:** Знайти межі системи

```javascript
export let options = {
  stages: [
    { duration: '2m', target: 100 },  // Розігрів
    { duration: '5m', target: 500 },  // Збільшення навантаження
    { duration: '2m', target: 1000 }, // Максимальне навантаження
    { duration: '5m', target: 1000 }, // Утримання піку
    { duration: '10m', target: 0 },   // Поступове зниження
  ],
  thresholds: {
    http_req_failed: ['rate<0.5'], // До 50% помилок під стресом прийнятно
  },
};
```

### 3. Spike Testing (Тестування сплесків)
**Мета:** Перевірити реакцію на раптові сплески навантаження

```javascript
export let options = {
  stages: [
    { duration: '10s', target: 100 }, // Нормальне навантаження
    { duration: '1m', target: 2000 }, // Різкий сплеск
    { duration: '3m', target: 100 },  // Повернення до норми
    { duration: '10s', target: 0 },   // Завершення
  ],
};
```

### 4. Volume Testing (Об'ємне тестування)
**Мета:** Тестування з великими об'ємами даних

```javascript
export let options = {
  stages: [
    { duration: '30m', target: 50 }, // Довготривалий тест
  ],
  thresholds: {
    http_req_duration: ['p(99)<2000'], // 99% запитів < 2s
    data_sent: ['count>1000000'],      // Відправлено > 1MB даних
  },
};
```

## Тестування REST API

### Базовий тест публікації повідомлень:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Кастомні метрики
export let successRate = new Rate('successful_publishes');
export let publishLatency = new Trend('publish_latency');

export default function () {
  const baseUrl = 'http://localhost:5000';
  
  // Тест AMQP публікації
  const amqpPayload = {
    content: `Test message ${__VU}-${__ITER}`,
    routingKey: 'test.performance',
    exchange: 'test-exchange',
    priority: Math.floor(Math.random() * 10),
    headers: {
      'test-id': `perf-${__VU}-${__ITER}`,
      'timestamp': new Date().toISOString()
    }
  };

  const amqpResponse = http.post(
    `${baseUrl}/api/amqp/publish`,
    JSON.stringify(amqpPayload),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );

  // Перевірки для AMQP
  const amqpSuccess = check(amqpResponse, {
    'AMQP publish status is 200': (r) => r.status === 200,
    'AMQP response time < 500ms': (r) => r.timings.duration < 500,
    'AMQP response has messageId': (r) => r.json('messageId') !== undefined,
  });

  successRate.add(amqpSuccess);
  publishLatency.add(amqpResponse.timings.duration);

  sleep(1); // Пауза між запитами
}
```

### Тест з різними типами навантаження:

```javascript
export let options = {
  scenarios: {
    // Постійне навантаження
    constant_load: {
      executor: 'constant-vus',
      vus: 50,
      duration: '10m',
      tags: { test_type: 'load' },
    },
    
    // Рампове навантаження
    ramping_load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 20 },
        { duration: '5m', target: 20 },
        { duration: '2m', target: 100 },
        { duration: '5m', target: 100 },
        { duration: '10m', target: 0 },
      ],
      tags: { test_type: 'stress' },
    },
    
    // Тест з фіксованою кількістю ітерацій
    shared_iterations: {
      executor: 'shared-iterations',
      vus: 10,
      iterations: 1000,
      tags: { test_type: 'volume' },
    },
  },
  
  thresholds: {
    http_req_duration: [
      'p(50)<200',   // 50% запитів < 200ms
      'p(95)<500',   // 95% запитів < 500ms
      'p(99)<1000',  // 99% запитів < 1s
    ],
    http_req_failed: ['rate<0.05'], // < 5% помилок
    successful_publishes: ['rate>0.95'], // > 95% успішних публікацій
  },
};
```

## Тестування MQTT брокера

### MQTT навантажувальний тест:

```javascript
import mqtt from 'k6/x/mqtt';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '1m', target: 10 },
    { duration: '3m', target: 50 },
    { duration: '1m', target: 0 },
  ],
};

const brokerUrl = 'mqtt://localhost:1883';

export default function () {
  const client = mqtt.connect(brokerUrl, {
    clientId: `k6-client-${__VU}`,
    keepAlive: 30,
    clean: true,
  });

  check(client, {
    'MQTT connection successful': (c) => c.connected === true,
  });

  // Публікація повідомлень з різними QoS
  const topics = [
    { topic: 'test/qos0', qos: 0 },
    { topic: 'test/qos1', qos: 1 },
    { topic: 'test/qos2', qos: 2 },
  ];

  topics.forEach(({ topic, qos }) => {
    const message = JSON.stringify({
      vu: __VU,
      iter: __ITER,
      timestamp: Date.now(),
      data: `Test message with QoS ${qos}`,
    });

    const publishResult = client.publish(topic, message, {
      qos: qos,
      retain: false,
    });

    check(publishResult, {
      [`Publish to ${topic} successful`]: (r) => r.error === undefined,
    });
  });

  client.disconnect();
}
```

## Моніторинг та метрики

### Вбудовані метрики k6:

#### HTTP метрики:
- **http_req_duration** - час відповіді
- **http_req_failed** - відсоток помилок
- **http_req_receiving** - час отримання відповіді
- **http_req_sending** - час відправлення запиту
- **http_req_waiting** - час очікування

#### Загальні метрики:
- **vus** - активні віртуальні користувачі
- **vus_max** - максимум віртуальних користувачів
- **iterations** - загальна кількість ітерацій
- **data_received** - отримано даних
- **data_sent** - відправлено даних

### Кастомні метрики для messaging:

```javascript
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';

// Лічильники
export let amqpMessagesPublished = new Counter('amqp_messages_published');
export let mqttMessagesPublished = new Counter('mqtt_messages_published');

// Відсотки успіху
export let amqpSuccessRate = new Rate('amqp_success_rate');
export let mqttSuccessRate = new Rate('mqtt_success_rate');

// Тренди (часові метрики)
export let messageProcessingTime = new Trend('message_processing_time');
export let queueDepth = new Trend('queue_depth');

// Індикатори
export let activeConnections = new Gauge('active_connections');
```

## Інтеграція з Grafana

### Налаштування InfluxDB для k6:

```javascript
export let options = {
  ext: {
    influxdb: {
      url: 'http://localhost:8086',
      database: 'k6',
      tags: {
        testType: 'performance',
        environment: 'development',
        version: 'v1.0.0',
      },
    },
  },
};
```

### Dashboard метрики:
- **Response Time Distribution** - розподіл часу відповіді
- **Request Rate** - кількість запитів на секунду
- **Error Rate** - відсоток помилок
- **Active Virtual Users** - активні користувачі
- **Message Queue Depth** - глибина черг
- **System Resources** - CPU, RAM, Network

## Сценарії тестування

### 1. Smoke Test (Димовий тест)
**Мета:** Швидка перевірка основної функціональності

```javascript
export let options = {
  vus: 1,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate==0'], // Жодних помилок
  },
};
```

### 2. Baseline Test (Базовий тест)
**Мета:** Встановлення базових показників

```javascript
export let options = {
  stages: [
    { duration: '5m', target: 10 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<200'],
  },
};
```

### 3. Peak Load Test (Тест пікового навантаження)
**Мета:** Тестування в період максимального навантаження

```javascript
export let options = {
  stages: [
    { duration: '1h', target: 500 }, // Піковий час
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.1'],
  },
};
```

## Аналіз результатів

### Ключові показники продуктивності (KPI):

#### Для REST API:
- **Throughput**: > 1000 RPS (requests per second)
- **Response Time**: p95 < 500ms
- **Error Rate**: < 1%
- **Availability**: > 99.9%

#### Для MQTT:
- **Message Rate**: > 10,000 msg/sec
- **Connection Time**: < 100ms
- **QoS 1 Delivery**: > 99.9%
- **QoS 2 Processing**: < 5s

#### Для AMQP:
- **Message Throughput**: > 5,000 msg/sec
- **Queue Processing**: < 10ms latency
- **Routing Efficiency**: > 95%
- **Memory Usage**: < 1GB for 100K messages

### Аналіз помилок:

```javascript
export function handleSummary(data) {
  const errors = data.metrics.http_req_failed;
  const duration = data.metrics.http_req_duration;
  
  console.log(`
    Performance Test Summary:
    ========================
    Total Requests: ${data.metrics.http_reqs.count}
    Failed Requests: ${errors.count} (${(errors.rate * 100).toFixed(2)}%)
    Average Response Time: ${duration.avg.toFixed(2)}ms
    95th Percentile: ${duration['p(95)'].toFixed(2)}ms
    99th Percentile: ${duration['p(99)'].toFixed(2)}ms
  `);
  
  return {
    'summary.json': JSON.stringify(data, null, 2),
  };
}
```

## Оптимізація продуктивності

### 1. Налаштування системи

#### Операційна система:
```bash
# Збільшити ліміти файлових дескрипторів
ulimit -n 65536

# Налаштування TCP
echo 'net.core.somaxconn = 65536' >> /etc/sysctl.conf
echo 'net.ipv4.tcp_max_syn_backlog = 65536' >> /etc/sysctl.conf
```

#### .NET додаток:
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 1000,
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

### 2. RabbitMQ оптимізація

```bash
# Збільшити ліміти пам'яті
rabbitmqctl set_vm_memory_high_watermark 0.8

# Оптимізувати диск
rabbitmqctl set_disk_free_limit 2GB

# Налаштувати cluster
rabbitmqctl set_policy ha-all ".*" '{"ha-mode":"all"}'
```

### 3. Моніторинг ресурсів

```javascript
// Додати в k6 тест
import { check } from 'k6';

export default function () {
  // Ваш основний код тесту...
  
  // Перевірка використання пам'яті
  const memoryUsage = getCurrentMemoryUsage(); // Функція отримання метрик
  
  check(null, {
    'Memory usage < 80%': () => memoryUsage < 0.8,
    'CPU usage < 70%': () => getCurrentCpuUsage() < 0.7,
  });
}
```

## CI/CD інтеграція

### GitHub Actions workflow:

```yaml
name: Performance Tests

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  performance-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Start services
      run: docker-compose up -d
      
    - name: Wait for services
      run: sleep 30
      
    - name: Install k6
      run: |
        curl -L https://github.com/grafana/k6/releases/download/v0.46.0/k6-v0.46.0-linux-amd64.tar.gz | tar xvz
        sudo mv k6-v0.46.0-linux-amd64/k6 /usr/local/bin/
        
    - name: Run smoke tests
      run: k6 run k6-tests/smoke-test.js
      
    - name: Run load tests
      run: k6 run k6-tests/api-performance.js
      
    - name: Archive test results
      uses: actions/upload-artifact@v3
      with:
        name: k6-results
        path: k6-results/
```

## Рекомендації та найкращі практики

### 1. Планування тестів
- Починайте з smoke tests
- Встановіть базові показники
- Поступово збільшуйте навантаження
- Тестуйте в умовах, близьких до production

### 2. Дизайн тестів
- Використовуйте реалістичні дані
- Імітуйте справжню поведінку користувачів
- Тестуйте різні сценарії використання
- Включайте негативні тести

### 3. Аналіз результатів
- Зосереджуйтесь на користувацькому досвіді
- Аналізуйте тренди, а не лише миттєві значення
- Корелюйте метрики продуктивності з бізнес-метриками
- Документуйте всі знахідки

### 4. Автоматизація
- Інтегруйте з CI/CD pipeline
- Налаштуйте автоматичні алерти
- Використовуйте trending для виявлення деградації
- Регулярно переглядайте та оновлюйте тести

## Поширені проблеми та рішення

### Проблема: Високий час відповіді
**Діагностика:**
- Перевірте CPU/Memory usage
- Аналізуйте database queries
- Перевірте network latency

**Рішення:**
- Оптимізуйте алгоритми
- Додайте кешування
- Масштабуйте горизонтально

### Проблема: Велика кількість помилок
**Діагностика:**
- Аналізуйте логи додатку
- Перевірте connection pool
- Моніторте queue overflow

**Рішення:**
- Впровадьте circuit breaker
- Збільште timeout values
- Додайте retry logic

Цей проект демонструє повний цикл тестування продуктивності messaging системи з використанням k6, покриваючи як REST API, так і протоколи AMQP/MQTT.
