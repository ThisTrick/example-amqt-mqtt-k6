import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');

// Configuration options
export const options = {
  scenarios: {
    // API Load Testing Scenario
    api_load_test: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '2m', target: 20 },  // Ramp up to 20 users over 2 minutes
        { duration: '5m', target: 20 },  // Stay at 20 users for 5 minutes
        { duration: '2m', target: 50 },  // Ramp up to 50 users over 2 minutes
        { duration: '5m', target: 50 },  // Stay at 50 users for 5 minutes
        { duration: '2m', target: 0 },   // Ramp down to 0 users
      ],
    },
    // Spike Testing Scenario
    spike_test: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 10 },   // Normal load
        { duration: '30s', target: 100 }, // Spike to 100 users
        { duration: '1m', target: 10 },   // Back to normal
      ],
      startTime: '16m', // Start after load test
    }
  },
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.1'],    // Error rate must be below 10%
    errors: ['rate<0.1'],             // Custom error rate must be below 10%
  },
};

// Test configuration
const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:8080';

// Test data for AMQP messages
const amqpTestData = {
  content: 'Load test message from k6',
  routingKey: 'test.load',
  exchange: 'messaging.exchange',
  priority: 1,
  persistent: true,
  headers: {
    'test-id': 'k6-load-test',
    'timestamp': new Date().toISOString()
  }
};

// Test data for MQTT messages
const mqttTestData = {
  content: 'MQTT load test message from k6',
  topic: 'messaging/test/load',
  qosLevel: 1,
  retain: false,
  headers: {
    'test-id': 'k6-mqtt-load-test',
    'timestamp': new Date().toISOString()
  }
};

export default function () {
  const testScenario = Math.random();
  
  if (testScenario < 0.5) {
    // Test AMQP publishing (50% of requests)
    testAmqpPublish();
  } else {
    // Test MQTT publishing (50% of requests)
    testMqttPublish();
  }
  
  // Random health check (10% of requests)
  if (Math.random() < 0.1) {
    testHealthCheck();
  }
  
  // Random metrics check (5% of requests)
  if (Math.random() < 0.05) {
    testMetricsEndpoint();
  }
  
  sleep(1); // Wait 1 second between iterations
}

function testAmqpPublish() {
  const url = `${BASE_URL}/amqp/publish`;
  const payload = JSON.stringify(amqpTestData);
  
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': generateCorrelationId(),
    },
  };
  
  const response = http.post(url, payload, params);
  
  const result = check(response, {
    'AMQP publish status is 200': (r) => r.status === 200,
    'AMQP publish response time < 500ms': (r) => r.timings.duration < 500,
    'AMQP publish has correlation ID': (r) => r.headers['X-Correlation-ID'] !== undefined,
  });
  
  errorRate.add(!result);
  responseTime.add(response.timings.duration);
}

function testMqttPublish() {
  const url = `${BASE_URL}/mqtt/publish`;
  const payload = JSON.stringify(mqttTestData);
  
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': generateCorrelationId(),
    },
  };
  
  const response = http.post(url, payload, params);
  
  const result = check(response, {
    'MQTT publish status is 200': (r) => r.status === 200,
    'MQTT publish response time < 500ms': (r) => r.timings.duration < 500,
    'MQTT publish has correlation ID': (r) => r.headers['X-Correlation-ID'] !== undefined,
  });
  
  errorRate.add(!result);
  responseTime.add(response.timings.duration);
}

function testHealthCheck() {
  const url = `${BASE_URL}/health`;
  
  const params = {
    headers: {
      'X-Correlation-ID': generateCorrelationId(),
    },
  };
  
  const response = http.get(url, params);
  
  const result = check(response, {
    'Health check status is 200': (r) => r.status === 200,
    'Health check response time < 100ms': (r) => r.timings.duration < 100,
  });
  
  errorRate.add(!result);
}

function testMetricsEndpoint() {
  const url = `${BASE_URL}/metrics`;
  
  const params = {
    headers: {
      'X-Correlation-ID': generateCorrelationId(),
    },
  };
  
  const response = http.get(url, params);
  
  const result = check(response, {
    'Metrics status is 200': (r) => r.status === 200,
    'Metrics response time < 200ms': (r) => r.timings.duration < 200,
  });
  
  errorRate.add(!result);
}

function generateCorrelationId() {
  return 'k6-' + Math.random().toString(36).substring(2, 15);
}

// Teardown function to log results
export function teardown(data) {
  console.log('Load test completed');
  console.log(`Total requests: ${data}`);
}