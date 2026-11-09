import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Custom Metrics
const successfulRequests = new Counter('successful_requests');
const failedRequests = new Rate('failed_requests_rate');
const readLatencyTrend = new Trend('read_latency_p99');
const writeLatencyTrend = new Trend('write_latency_p99');

// Test Configuration: 1,000 Concurrent Users (CCU) Load Test
export const options = {
  stages: [
    { duration: '30s', target: 200 },   // Warm-up ramp-up to 200 CCU
    { duration: '1m', target: 500 },    // Scale to 500 CCU
    { duration: '2m', target: 1000 },   // Peak Stress at 1,000 CCU
    { duration: '1m', target: 1000 },   // Steady State at 1,000 CCU
    { duration: '30s', target: 0 },     // Ramp-down to 0 CCU
  ],
  thresholds: {
    // SLAs: Read P99 < 30ms (target) or < 100ms (max under peak 1k CCU)
    'http_req_duration{type:read}': ['p(95)<30', 'p(99)<80'],
    'http_req_duration{type:write}': ['p(95)<60', 'p(99)<120'],
    'failed_requests_rate': ['rate<0.01'], // Error rate < 1%
    'http_req_failed': ['rate<0.01'],
  },
};

const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5000';

const HEADERS = {
  'Content-Type': 'application/json',
  'Authorization': 'Bearer superadmin',
  'X-Correlation-ID': 'k6-loadtest-runner',
};

export default function () {
  const userId = `USR-${Math.floor(Math.random() * 50) + 1}`;

  // 1. SCENARIO: Read Heavy (CQRS Dapper + Multi-Level L1/L2 Cache)
  group('01_ReadHeavy_FastInboxAndDirectory', () => {
    // 1.1 Read User Pending Tasks via Dapper SequentialAccess
    const taskStart = Date.now();
    const taskRes = http.get(`${BASE_URL}/api/approval-tasks/pending`, {
      headers: { ...HEADERS, 'X-Current-User': userId },
      tags: { type: 'read' },
    });
    readLatencyTrend.add(Date.now() - taskStart);

    check(taskRes, {
      'Tasks query status is 200': (r) => r.status === 200,
      'Response has X-Correlation-ID': (r) => r.headers['X-Correlation-Id'] !== undefined,
    });

    // 1.2 Read Org Directory (L1 Memory / L2 Redis Multi-Level Cached)
    const orgRes = http.get(`${BASE_URL}/api/org-units`, {
      headers: HEADERS,
      tags: { type: 'read' },
    });

    check(orgRes, {
      'Org units status is 200': (r) => r.status === 200,
    });

    // 1.3 Health Readiness Check
    const healthRes = http.get(`${BASE_URL}/healthz/ready`, {
      headers: HEADERS,
      tags: { type: 'read' },
    });

    check(healthRes, {
      'Health ready status is 200': (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // 2. SCENARIO: Write Flow (Form Submission + Outbox Message Integration)
  group('02_Write_WorkflowSubmission', () => {
    const payload = JSON.stringify({
      formKey: 'FORM-PAYMENT-REQUEST',
      formDataJson: JSON.stringify({
        amount: 25000000,
        purpose: 'K6 Stress Test Transaction',
        beneficiary: 'Vendor ABC Vietnam',
        submittedAt: new Date().toISOString(),
      }),
      departmentId: 'D0000000-0000-0000-0000-000000000001',
      priority: 'Normal',
      notes: 'Load testing submission with Transactional Outbox',
    });

    const writeStart = Date.now();
    const submitRes = http.post(`${BASE_URL}/api/workflow/submissions`, payload, {
      headers: { ...HEADERS, 'X-Current-User': userId },
      tags: { type: 'write' },
    });
    writeLatencyTrend.add(Date.now() - writeStart);

    const isOk = check(submitRes, {
      'Submission created status is 200 or 201': (r) => r.status === 200 || r.status === 201,
    });

    if (isOk) {
      successfulRequests.add(1);
      failedRequests.add(0);
    } else {
      failedRequests.add(1);
    }
  });

  sleep(1);
}

