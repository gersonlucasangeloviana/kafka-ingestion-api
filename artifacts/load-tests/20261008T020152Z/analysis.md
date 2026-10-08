# Public VPS test at 1,000 req/s

Run started on October 7, 2026 at 23:01:52 São Paulo time (October 8 at 02:01:52 UTC). [Run parameters](run.json): 100 req/s warmup for 15 seconds, followed by 1,000 req/s for 60 seconds, 600 preallocated VUs with a fixed maximum of 600. Target: `https://api-kafka.vianadev.com.br`.

## Result

| Metric | Observed |
|---|---:|
| Completed iterations / confirmed publications | 59,738 |
| Dropped iterations | 263 |
| Dropped share of completed + dropped | approximately 0.438% |
| HTTP publication errors | 0% |
| Kafka confirmations among sent requests | 100% |
| Publication HTTP p95 | 22.08 ms |
| Publication HTTP p99 | 142.38 ms |
| Maximum publication HTTP duration | 3,839.86 ms |
| Complete iteration p99 | 638.66 ms |
| Maximum complete iteration duration | 7,625.93 ms |
| Maximum HTTP blocked time | 7,605.93 ms |
| Maximum TCP connection time | 2,412.72 ms |
| Maximum TLS handshake time | 7,004.41 ms |

Only the zero-dropped-iteration threshold failed. The measurement ran for its full minute, without interrupted iterations; k6 then returned a failure status and the runner recorded the failed stage. Original evidence: [terminal log](1000.log), [JSON summary](1000.json), [comparison report](results.md).

## What the evidence supports

At 23:02:10 the terminal explicitly reported `Insufficient VUs, reached 600 active VUs and cannot initialize more`. The first seconds show hundreds of active VUs; from roughly scenario second 7 onward, the displayed samples show 13–28 active VUs, and successive completion samples increase by about 1,000 per second. These observations support a transient startup problem rather than proving a continuously saturated broker. The log does not timestamp each dropped iteration, so it cannot prove that all 263 drops occurred at startup.

In the constant-arrival-rate executor, unavailable VUs prevent a scheduled iteration from starting. Dropped iterations are not failed HTTP responses or lost Kafka records. Low HTTP latency percentiles do not include the time establishing TCP/TLS connections; complete-iteration and connection metrics show a much larger startup cost. Sources: [Grafana dropped iterations](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/dropped-iterations/), [metric definitions](https://grafana.com/docs/k6/latest/using-k6/metrics/reference/).

The runner launches warmup and measurement with separate `k6 run` commands. Warmup can prepare the API/broker, but connections from that process do not survive into the measurement process. The measurement immediately requests 1,000 iterations per second with new client connections. This is a relevant limitation of the current benchmark design. A TLS spike may originate in the generator, network or proxy; these results cannot locate the bottleneck or exclude server-side effects.

## Capacity conclusion and next experiment

This execution confirms 59,738 acknowledged publications and approximately 1,000 completions per second during much of the minute. It does not validate a zero-drop stage at 1,000 req/s or identify the maximum sustained capacity. The highest fully passed measured stage recorded so far is 400 req/s; this is a validated test point, not a demonstrated upper bound.

Before raising the rate again, use a gradual arrival-rate ramp in the same k6 execution, explicitly separate startup/ramp evidence from the steady measurement window, and capture per-second dropped iterations and connection/iteration latency alongside generator and VPS utilization. Repeat the steady stage at least three times, then run the highest passing rate for 10 minutes. Preserve the startup result above rather than retroactively relabeling it as passed.
