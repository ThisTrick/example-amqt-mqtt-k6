import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics for MQTT performance tracking
const errorRate = new Rate('mqtt_errors');
const messageLatency = new Trend('mqtt_message_latency');
const messagesPublished = new Counter('mqtt_messages_published');
const qosLatency = new Trend('mqtt_qos_latency');

// MQTT Performance Test Configuration
export const options = {
  scenarios: {
    // High-throughput MQTT publishing
    mqtt_throughput: {
      executor: 'constant-arrival-rate',
      rate: 150, // 150 messages per second (MQTT typically handles more)
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 25,
      maxVUs: 60,
    },
    // MQTT QoS testing
    mqtt_qos_levels: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 30 },  // QoS 0 testing
        { duration: '2m', target: 30 },  // Mixed QoS testing
        { duration: '1m', target: 20 },  // QoS 1 focus
        { duration: '2m', target: 20 },  // QoS 2 testing
        { duration: '1m', target: 0 },   // Ramp down
      ],
      startTime: '6m', // Start after throughput test
    },
    // MQTT burst and retain testing
    mqtt_burst_retain: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      stages: [
        { duration: '1m', target: 300 }, // Burst to 300 msg/s
        { duration: '2m', target: 600 }, // Peak at 600 msg/s
        { duration: '2m', target: 300 }, // Scale back
        { duration: '1m', target: 50 },  // Normal rate
      ],
      preAllocatedVUs: 40,
      maxVUs: 120,
      startTime: '13m', // Start after QoS test
    }
  },
  thresholds: {
    // MQTT specific thresholds
    http_req_duration: ['p(95)<800', 'p(99)<1500'], // MQTT should be faster than AMQP
    http_req_failed: ['rate<0.03'],      // Lower error rate for MQTT
    mqtt_errors: ['rate<0.03'],          // MQTT error rate must be below 3%
    mqtt_message_latency: ['p(95)<300'], // 95% of MQTT messages under 300ms
    mqtt_qos_latency: ['p(90)<400'],     // QoS processing under 400ms
  },
};

// Test configuration
const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:8080';
const MQTT_ENDPOINT = `${BASE_URL}/mqtt/publish`;

// MQTT message templates for different QoS levels and scenarios
const messageTemplates = {
  qos0: {
    content: 'QoS 0 - Fire and forget message',
    topic: 'messaging/perf/qos0',
    qosLevel: 0,
    retain: false,
  },
  qos1: {
    content: 'QoS 1 - Acknowledged delivery message',
    topic: 'messaging/perf/qos1',
    qosLevel: 1,
    retain: false,
  },
  qos2: {
    content: 'QoS 2 - Exactly once delivery message',
    topic: 'messaging/perf/qos2',
    qosLevel: 2,
    retain: false,
  },
  retained_small: {
    content: 'Small retained message for topic persistence',
    topic: 'messaging/perf/retained/small',
    qosLevel: 1,
    retain: true,
  },
  retained_large: {
    content: 'L'.repeat(5000), // 5KB retained message
    topic: 'messaging/perf/retained/large',
    qosLevel: 1,
    retain: true,
  },
  sensor_data: {
    content: JSON.stringify({
      temperature: Math.random() * 40 - 10, // -10 to 30°C
      humidity: Math.random() * 100,        // 0 to 100%
      pressure: 900 + Math.random() * 200,  // 900 to 1100 hPa
      timestamp: Date.now(),
      sensorId: `sensor-${Math.floor(Math.random() * 1000)}`
    }),
    topic: 'messaging/sensors/environmental',
    qosLevel: 1,
    retain: false,
  },
  telemetry: {
    content: JSON.stringify({
      deviceId: `device-${Math.floor(Math.random() * 100)}`,
      battery: Math.random() * 100,
      signal: Math.random() * -30 - 40, // -70 to -40 dBm
      uptime: Math.floor(Date.now() / 1000),
      version: '1.0.0'
    }),
    topic: 'messaging/telemetry/status',
    qosLevel: 0,
    retain: false,
  }
};

export default function () {
  const messageType = selectMessageType();
  const testData = { ...messageTemplates[messageType] };
  
  // Add dynamic test metadata
  testData.headers = {
    'test-run-id': __ENV.TEST_RUN_ID || 'default',
    'vu-id': __VU,
    'iteration': __ITER,
    'timestamp': Date.now(),
    'correlation-id': generateCorrelationId(),
    'message-type': messageType
  };
  
  publishMqttMessage(testData, messageType);
  
  // Dynamic sleep based on QoS level
  const sleepTime = testData.qosLevel === 2 ? 0.1 : testData.qosLevel === 1 ? 0.05 : 0.02;
  sleep(sleepTime);
}

function selectMessageType() {
  const scenario = __ENV.EXEC_SCENARIO || 'mqtt_throughput';
  const rand = Math.random();
  
  switch (scenario) {
    case 'mqtt_qos_levels':
      if (rand < 0.4) return 'qos0';
      if (rand < 0.7) return 'qos1';
      return 'qos2';
      
    case 'mqtt_burst_retain':
      if (rand < 0.3) return 'retained_small';
      if (rand < 0.4) return 'retained_large';
      if (rand < 0.7) return 'sensor_data';
      return 'telemetry';
      
    default: // mqtt_throughput
      if (rand < 0.5) return 'qos0';
      if (rand < 0.8) return 'qos1';
      if (rand < 0.9) return 'sensor_data';
      return 'telemetry';
  }
}

function publishMqttMessage(testData, messageType) {
  const payload = JSON.stringify(testData);
  const startTime = Date.now();
  
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': testData.headers['correlation-id'],
      'X-Message-Type': messageType,
      'X-QoS-Level': testData.qosLevel.toString(),
      'X-Retain': testData.retain.toString(),
    },
    timeout: testData.qosLevel === 2 ? '8s' : '5s',
  };
  
  const response = http.post(MQTT_ENDPOINT, payload, params);
  const endTime = Date.now();
  const latency = endTime - startTime;
  
  // Record metrics
  messagesPublished.add(1);
  messageLatency.add(latency);
  
  // QoS-specific latency tracking
  if (testData.qosLevel > 0) {
    qosLatency.add(latency);
  }
  
  // Validate response
  const result = check(response, {
    [`MQTT ${messageType} message published successfully`]: (r) => r.status === 200,
    [`MQTT ${messageType} response time acceptable`]: (r) => {
      const maxTime = testData.qosLevel === 2 ? 1500 : testData.qosLevel === 1 ? 800 : 300;
      return r.timings.duration < maxTime;
    },
    'MQTT response has correlation ID': (r) => r.headers['X-Correlation-ID'] !== undefined,
    'MQTT response content is valid': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.success === true || body.status === 'published';
      } catch (e) {
        return false;
      }
    },
    [`MQTT QoS ${testData.qosLevel} processed correctly`]: (r) => {
      // Additional QoS-specific validations could be added here
      return r.status === 200;
    },
  });
  
  errorRate.add(!result);
  
  // Enhanced error logging for MQTT
  if (!result) {
    console.error(`MQTT publish failed: Status ${response.status}, Type: ${messageType}, QoS: ${testData.qosLevel}, Retain: ${testData.retain}, Duration: ${response.timings.duration}ms`);
  }
  
  // Log retained message confirmations occasionally
  if (testData.retain && Math.random() < 0.01) { // 1% of retained messages
    console.log(`Retained message published to ${testData.topic} with QoS ${testData.qosLevel}`);
  }
}

function generateCorrelationId() {
  return `k6-mqtt-${__VU}-${__ITER}-${Date.now()}`;
}

// Setup function to prepare MQTT test environment
export function setup() {
  console.log('Starting MQTT performance test');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`Test scenario: ${__ENV.EXEC_SCENARIO || 'mqtt_throughput'}`);
  console.log(`Test run ID: ${__ENV.TEST_RUN_ID || 'default'}`);
  
  // Health check before starting
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    console.error('Health check failed before starting MQTT performance test');
    return { healthy: false };
  }
  
  // Test MQTT endpoint availability
  const testMessage = {
    content: 'Pre-test connectivity check',
    topic: 'messaging/test/connectivity',
    qosLevel: 0,
    retain: false
  };
  
  const testResponse = http.post(MQTT_ENDPOINT, JSON.stringify(testMessage), {
    headers: { 'Content-Type': 'application/json' }
  });
  
  if (testResponse.status !== 200) {
    console.error('MQTT endpoint test failed before starting performance test');
    return { healthy: false };
  }
  
  return { 
    healthy: true, 
    startTime: Date.now(),
    scenario: __ENV.EXEC_SCENARIO || 'mqtt_throughput'
  };
}

// Teardown function with MQTT-specific cleanup
export function teardown(data) {
  if (!data.healthy) {
    console.log('Test was not executed due to failed health check');
    return;
  }
  
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`MQTT performance test (${data.scenario}) completed in ${duration} seconds`);
  console.log('Check k6 metrics and MQTT broker logs for detailed performance analysis');
  
  // Send cleanup retained message if we used retained messages
  if (data.scenario === 'mqtt_burst_retain') {
    console.log('Note: Retained messages may persist in the MQTT broker. Check broker configuration for cleanup policies.');
  }
}

// Custom summary for MQTT-specific metrics
export function handleSummary(data) {
  const scenario = __ENV.EXEC_SCENARIO || 'mqtt_throughput';
  
  return {
    [`mqtt-${scenario}-summary.json`]: JSON.stringify({
      scenario: scenario,
      mqtt_messages_published: data.metrics.mqtt_messages_published.values.count,
      mqtt_error_rate: data.metrics.mqtt_errors.values.rate,
      mqtt_avg_latency: data.metrics.mqtt_message_latency.values.avg,
      mqtt_p95_latency: data.metrics.mqtt_message_latency.values['p(95)'],
      mqtt_p99_latency: data.metrics.mqtt_message_latency.values['p(99)'],
      mqtt_qos_avg_latency: data.metrics.mqtt_qos_latency?.values.avg || 'N/A',
      mqtt_qos_p90_latency: data.metrics.mqtt_qos_latency?.values['p(90)'] || 'N/A',
      http_success_rate: (1 - data.metrics.http_req_failed.values.rate) * 100,
      test_duration: data.state.testRunDurationMs / 1000,
      messages_per_second: data.metrics.mqtt_messages_published.values.count / (data.state.testRunDurationMs / 1000)
    }, null, 2),
  };
}
