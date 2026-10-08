# First public VPS load ladder

Run started on October 7, 2026 at 22:45 São Paulo time (October 8 at 01:45 UTC). Target: `https://api-kafka.vianadev.com.br`. Evidence: original k6 JSON files and [generated report](results.md). This execution began before terminal-log and run-metadata capture was added to the runner.

Stages at 100, 200, 300 and 400 req/s passed every configured threshold for 60 seconds each. The 500 req/s stage completed with:

| Metric | Observed |
|---|---:|
| Completed iterations | 29,989 |
| Confirmed Kafka publications | 29,989 |
| HTTP publication error rate | 0% |
| Kafka confirmation rate | 100% |
| Publication HTTP p95 | 20.66 ms |
| Publication HTTP p99 | 134.11 ms |
| Dropped iterations | 12 |
| Dropped share of completed + dropped | approximately 0.04% |
| Maximum complete iteration duration | 894.49 ms |
| Maximum HTTP blocked time | 778.02 ms |
| Maximum TCP connection time | 407.63 ms |
| Maximum TLS handshake time | 471.75 ms |

Only `dropped_iterations: count==0` failed. For the constant-arrival-rate executor, dropped iterations are iterations that did not start because no virtual user was available; they are not rejected HTTP requests or lost Kafka messages. The runner correctly stopped before proceeding to 600 req/s. Source: [Grafana's dropped-iteration documentation](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/dropped-iterations/).

The script's default at 500 req/s preallocates 150 VUs and permits up to 750. The recorded `vus_max` is 162, consistent with allocating additional VUs during the stage. Reported active-VU gauges are sampled and cannot reconstruct transient availability. The connection/TLS spikes and extra VU allocation suggest transient client/connection overhead, but the aggregate summary has no timeline and does not establish the exact cause or exclude server/network delays.

The highest fully passed stage in this run is 400 req/s. The 500 req/s result does not establish the API's capacity limit: publication errors and latency thresholds passed, while the generator did not deliver every scheduled iteration. HTTP request-duration metrics exclude connection setup, so their low percentiles do not rule out longer complete iterations. Source: [Grafana metric definitions](https://grafana.com/docs/k6/latest/using-k6/metrics/reference/).

Next experiment: repeat only 500 req/s with 300 VUs preallocated and a fixed maximum of 300, preserving the 60-second duration and the zero-drop threshold. Record generator utilization, VPS utilization and timestamped connection/iteration metrics. Increasing preallocation can reduce allocation overhead; it is a diagnostic change, not proof that drops will disappear. Source: [Grafana's VU allocation guidance](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/arrival-rate-vu-allocation/).

```bash
BASE_URL=https://api-kafka.vianadev.com.br \
RATES='500' PRE_ALLOCATED_VUS=300 MAX_VUS=300 \
./scripts/run-load-tests.sh
```

Repeat the stage to assess reproducibility before advancing the ladder. No VPS CPU, memory, disk or generator-utilization timeline was captured in these k6 summaries.
