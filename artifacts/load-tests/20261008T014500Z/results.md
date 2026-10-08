# Load test results

Configured thresholds are a benchmark criterion, not a performance guarantee.

| Target req/s | Confirmed messages | Observed confirmations/s* | p95 ms | p99 ms | HTTP errors % | Ack % | Dropped | Passed |
|---:|---:|---:|---:|---:|---:|---:|---:|:---:|
| 100 | 6001 | 99.8 | 21.13 | 135.24 | 0.000 | 100.000 | 0 | Yes |
| 200 | 12000 | 199.6 | 20.68 | 134.89 | 0.000 | 100.000 | 0 | Yes |
| 300 | 18000 | 299.6 | 20.00 | 89.27 | 0.000 | 100.000 | 0 | Yes |
| 400 | 24001 | 399.4 | 20.46 | 133.54 | 0.000 | 100.000 | 0 | Yes |
| 500 | 29989 | 498.4 | 20.66 | 134.11 | 0.000 | 100.000 | 12 | No |

*Counter rate includes setup and graceful completion time. Compare counts and dropped iterations with the configured measurement duration.
