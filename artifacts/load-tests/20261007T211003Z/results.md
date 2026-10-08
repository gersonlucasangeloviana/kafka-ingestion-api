# Load test results

Configured thresholds are a benchmark criterion, not a performance guarantee.

| Target req/s | Confirmed messages | Observed confirmations/s* | p95 ms | p99 ms | HTTP errors % | Ack % | Dropped | Passed |
|---:|---:|---:|---:|---:|---:|---:|---:|:---:|
| 100 | 1001 | 99.9 | 9.92 | 10.88 | 0.000 | 100.000 | 0 | Yes |
| 200 | 2001 | 199.8 | 9.19 | 10.13 | 0.000 | 100.000 | 0 | Yes |
| 300 | 3001 | 299.7 | 9.02 | 9.80 | 0.000 | 100.000 | 0 | Yes |
| 400 | 4000 | 399.4 | 8.09 | 8.92 | 0.000 | 100.000 | 0 | Yes |
| 500 | 5000 | 499.3 | 8.31 | 9.47 | 0.000 | 100.000 | 0 | Yes |
| 600 | 6001 | 599.3 | 7.93 | 8.84 | 0.000 | 100.000 | 0 | Yes |
| 700 | 7000 | 699.4 | 7.70 | 8.51 | 0.000 | 100.000 | 0 | Yes |
| 800 | 8000 | 799.2 | 7.69 | 8.63 | 0.000 | 100.000 | 0 | Yes |
| 900 | 9001 | 899.0 | 7.51 | 8.73 | 0.000 | 100.000 | 0 | Yes |
| 1000 | 10000 | 999.0 | 7.11 | 7.97 | 0.000 | 100.000 | 0 | Yes |
| 3000 | 30001 | 2996.8 | 7.01 | 8.63 | 0.000 | 100.000 | 0 | Yes |
| 5000 | 50001 | 4993.4 | 7.09 | 10.66 | 0.000 | 100.000 | 0 | Yes |

*Counter rate includes setup and graceful completion time. Compare counts and dropped iterations with the configured measurement duration.
