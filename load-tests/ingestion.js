import http from 'k6/http';
import { check, fail } from 'k6';
import exec from 'k6/execution';
import { Counter, Rate } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '');
const apiKey = __ENV.API_KEY;
const rps = Number(__ENV.RPS || 100);
if (!apiKey || !Number.isInteger(rps) || rps < 1) {
  throw new Error('Set API_KEY and a positive integer RPS.');
}
const confirmed = new Counter('confirmed_messages');
const confirmations = new Rate('kafka_confirmations');

export const options = {
  discardResponseBodies: true,
  scenarios: {
    ingestion: {
      executor: 'constant-arrival-rate',
      rate: rps,
      timeUnit: '1s',
      duration: __ENV.DURATION || '60s',
      preAllocatedVUs: Number(__ENV.PRE_ALLOCATED_VUS || Math.max(50, Math.ceil(rps * 0.3))),
      maxVUs: Number(__ENV.MAX_VUS || Math.max(100, Math.ceil(rps * 1.5))),
      gracefulStop: '15s',
    },
  },
  thresholds: {
    'http_req_failed{endpoint:publish}': ['rate<0.001'],
    'http_req_duration{endpoint:publish}': [
      `p(95)<${__ENV.P95_MS || 200}`,
      `p(99)<${__ENV.P99_MS || 500}`,
    ],
    kafka_confirmations: ['rate>0.999'],
    dropped_iterations: ['count==0'],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

export function setup() {
  if (http.get(`${baseUrl}/health/ready`, { timeout: '10s', tags: { endpoint: 'readiness' } }).status !== 200) {
    fail('API is not ready.');
  }
}

export default function () {
  const id = `k6-${__ENV.RUN_ID || 'local'}-${rps}-${exec.scenario.iterationInTest}`;
  const response = http.post(`${baseUrl}/api/v1/messages`, JSON.stringify({ id }), {
    headers: { 'Content-Type': 'application/json', 'X-Api-Key': apiKey },
    tags: { endpoint: 'publish', name: 'POST /api/v1/messages' },
    timeout: '15s',
  });
  const success = check(response, { 'Kafka acknowledged publication': (r) => r.status === 200 });
  confirmations.add(success);
  if (success) confirmed.add(1);
}

export function handleSummary(data) {
  const filename = __ENV.SUMMARY_PATH || `artifacts/k6-${rps}.json`;
  return {
    [filename]: JSON.stringify(data, null, 2),
    stdout: JSON.stringify({
      targetRps: rps,
      confirmed: data.metrics.confirmed_messages?.values || { count: 0, rate: 0 },
      droppedIterations: data.metrics.dropped_iterations?.values?.count || 0,
      latency: data.metrics['http_req_duration{endpoint:publish}']?.values,
      thresholdsPassed: Object.values(data.metrics).every((metric) =>
        Object.values(metric.thresholds || {}).every((threshold) => threshold.ok)),
    }, null, 2) + '\n',
  };
}
