# Load test results

Configured thresholds are a benchmark criterion, not a performance guarantee.

| Target req/s | Confirmed messages | Observed confirmations/s* | p95 ms | p99 ms | HTTP errors % | Ack % | Dropped | Passed |
|---:|---:|---:|---:|---:|---:|---:|---:|:---:|
| 1000 | 59738 | 984.8 | 22.08 | 142.38 | 0.000 | 100.000 | 263 | No |

*Counter rate includes setup and graceful completion time. Compare counts and dropped iterations with the configured measurement duration.
