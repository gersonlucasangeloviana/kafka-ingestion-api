# Load test results

Configured thresholds are a benchmark criterion, not a performance guarantee.

| Target req/s | Confirmed messages | Observed confirmations/s* | p95 ms | p99 ms | HTTP errors % | Ack % | Dropped | Passed |
|---:|---:|---:|---:|---:|---:|---:|---:|:---:|
| 10 | 151 | 10.0 | 24.76 | 48.11 | 0.000 | 100.000 | 0 | Yes |
| 50 | 751 | 49.8 | 22.50 | 138.63 | 0.000 | 100.000 | 0 | Yes |
| 100 | 1501 | 99.5 | 21.50 | 137.73 | 0.000 | 100.000 | 0 | Yes |

*Counter rate includes setup and graceful completion time. Compare counts and dropped iterations with the configured measurement duration.
