import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics for AMQP performance tracking
const errorRate = new Rate('amqp_errors');
const messageLatency = new Trend('amqp_message_latency');
const messagesPublished = new Counter('amqp_messages_published');

// AMQP Performance Test Configuration
export const options = {
  scenarios: {
    // High-throughput AMQP publishing
    amqp_throughput: {
      executor: 'constant-arrival-rate',
      rate: 100, // 100 messages per second
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 20,
      maxVUs: 50,
    },
    // AMQP burst testing
    amqp_burst: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      stages: [
        { duration: '1m', target: 200 }, // Ramp up to 200 msg/s
        { duration: '2m', target: 500 }, // Ramp up to 500 msg/s
        { duration: '2m', target: 200 }, // Ramp down to 200 msg/s
        { duration: '1m', target: 50 },  // Ramp down to 50 msg/s
      ],
      preAllocatedVUs: 30,
      maxVUs: 100,
      startTime: '6m', // Start after throughput test
    },
    // AMQP stress testing
    amqp_stress: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '2m', target: 100 }, // Ramp up to 100 VUs
        { duration: '5m', target: 100 }, // Stay at 100 VUs
        { duration: '2m', target: 200 }, // Ramp up to 200 VUs
        { duration: '3m', target: 200 }, // Stay at 200 VUs
        { duration: '2m', target: 0 },   // Ramp down
      ],
      startTime: '12m', // Start after burst test
    }
  },
  thresholds: {
    // AMQP specific thresholds
    http_req_duration: ['p(95)<1000', 'p(99)<2000'], // 95% under 1s, 99% under 2s
    http_req_failed: ['rate<0.05'],    // Error rate must be below 5%
    amqp_errors: ['rate<0.05'],        // AMQP error rate must be below 5%
    amqp_message_latency: ['p(95)<500'], // 95% of AMQP messages under 500ms
  },
};

// Test configuration
const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:8080';
const AMQP_ENDPOINT = `${BASE_URL}/amqp/publish`;

// AMQP message templates for different test scenarios
const messageTemplates = {
  small: {
    content: 'Small AMQP test message',
    routingKey: 'perf.test.small',
    exchange: 'messaging.exchange',
    priority: 1,
    persistent: false,
  },
  medium: {
    content: 'A'.repeat(1000), // 1KB message
    routingKey: 'perf.test.medium',
    exchange: 'messaging.exchange',
    priority: 2,
    persistent: true,
    headers: {
      'message-size': 'medium',
      'test-type': 'performance'
    }
  },
  large: {
    content: 'B'.repeat(10000), // 10KB message
    routingKey: 'perf.test.large',
    exchange: 'messaging.exchange',
    priority: 3,
    persistent: true,
    headers: {
      'message-size': 'large',
      'test-type': 'performance',
      'content-encoding': 'utf-8'
    }
  }
};

export default function () {
  const messageType = selectMessageType();
  const testData = messageTemplates[messageType];
  
  // Add dynamic test data
  testData.headers = {
    ...testData.headers,
    'test-run-id': __ENV.TEST_RUN_ID || 'default',
    'vu-id': __VU,
    'iteration': __ITER,
    'timestamp': Date.now(),
    'correlation-id': generateCorrelationId()
  };
  
  publishAmqpMessage(testData, messageType);
  
  // Variable sleep based on message type
  const sleepTime = messageType === 'large' ? 0.1 : 0.05;
  sleep(sleepTime);
}

function selectMessageType() {
  const rand = Math.random();
  if (rand < 0.7) return 'small';      // 70% small messages
  if (rand < 0.9) return 'medium';     // 20% medium messages
  return 'large';                      // 10% large messages
}

function publishAmqpMessage(testData, messageType) {
  const payload = JSON.stringify(testData);
  const startTime = Date.now();
  
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': testData.headers['correlation-id'],
      'X-Message-Type': messageType,
    },
    timeout: messageType === 'large' ? '10s' : '5s',
  };
  
  const response = http.post(AMQP_ENDPOINT, payload, params);
  const endTime = Date.now();
  const latency = endTime - startTime;
  
  // Record metrics
  messagesPublished.add(1);
  messageLatency.add(latency);
  
  // Validate response
  const result = check(response, {
    [`AMQP ${messageType} message published successfully`]: (r) => r.status === 200,
    [`AMQP ${messageType} response time acceptable`]: (r) => {
      const maxTime = messageType === 'large' ? 2000 : messageType === 'medium' ? 1000 : 500;
      return r.timings.duration < maxTime;
    },
    'AMQP response has correlation ID': (r) => r.headers['X-Correlation-ID'] !== undefined,
    'AMQP response content is valid': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.success === true || body.status === 'published';
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(!result);
  
  // Log detailed errors for debugging
  if (!result) {
    console.error(`AMQP publish failed: Status ${response.status}, Message: ${messageType}, Duration: ${response.timings.duration}ms`);
  }
}

function generateCorrelationId() {
  return `k6-amqp-${__VU}-${__ITER}-${Date.now()}`;
}

// Setup function to prepare test environment
export function setup() {
  console.log('Starting AMQP performance test');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`Test run ID: ${__ENV.TEST_RUN_ID || 'default'}`);
  
  // Health check before starting
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    console.error('Health check failed before starting AMQP performance test');
    return { healthy: false };
  }
  
  return { healthy: true, startTime: Date.now() };
}

// Teardown function to log results and cleanup
export function teardown(data) {
  if (!data.healthy) {
    console.log('Test was not executed due to failed health check');
    return;
  }
  
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`AMQP performance test completed in ${duration} seconds`);
  console.log('Check k6 metrics for detailed performance analysis');
}

// Custom summary for AMQP-specific metrics
export function handleSummary(data) {
  return {
    'amqp-performance-summary.json': JSON.stringify({
      amqp_messages_published: data.metrics.amqp_messages_published.values.count,
      amqp_error_rate: data.metrics.amqp_errors.values.rate,
      amqp_avg_latency: data.metrics.amqp_message_latency.values.avg,
      amqp_p95_latency: data.metrics.amqp_message_latency.values['p(95)'],
      amqp_p99_latency: data.metrics.amqp_message_latency.values['p(99)'],
      http_success_rate: (1 - data.metrics.http_req_failed.values.rate) * 100,
      test_duration: data.state.testRunDurationMs / 1000,
    }, null, 2),
  };
}
