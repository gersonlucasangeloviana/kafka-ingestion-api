# Local smoke test — October 7, 2026

This is a short functional load validation, **not a VPS capacity benchmark**. All 12 stages passed, with Kafka delivery acknowledgements and zero dropped iterations. Each stage ran once for 10 seconds, without a dedicated warmup or cooldown. Generator, API and broker shared the same computer; other containers were running.

- Host: Apple M4 Pro, 14 CPU cores, 24 GiB RAM, macOS 15.7.
- Docker VM: 14 CPUs and approximately 7.75 GiB RAM; no service-specific CPU/memory quotas.
- API: Linux ARM64 container, .NET/ASP.NET Core 10.0.12, Confluent.Kafka 2.16.0.
- Kafka: `apache/kafka:4.1.2`, Linux ARM64, one KRaft broker, 6 partitions, replication factor 1, 512 MiB JVM heap.
- Producer: idempotence, `acks=all`, LZ4, 5 ms linger; HTTP success waits for acknowledged delivery.
- Network: local plain HTTP through the Docker host port; no Traefik, TLS or remote network hop.
- Generator: k6 2.1.0, constant arrival rate, default virtual-user allocation.
- Timestamp: October 7, 2026, 18:10 São Paulo time (21:10 UTC).

Command:

```bash
BASE_URL=http://localhost:8085 DURATION=10s WARMUP=false COOLDOWN_SECONDS=0 \
  ./scripts/run-load-tests.sh
```

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
