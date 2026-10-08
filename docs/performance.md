# Performance protocol

## Measurement

The configured request rate is offered load, not achieved throughput. A valid stage has zero dropped iterations, acceptable latency/error rates and Kafka-confirmed responses. Report target rate, confirmed count, observed counter rate, HTTP failures, p95/p99 and dropped iterations together.

The built-in counter rate includes small setup/graceful-completion overhead. For a fixed 60-second scenario with no drops, compare confirmed count with `target × 60`; preserve raw summaries for analysis. Aborted runs and setup failures must not be interpreted as successful capacity measurements.

## Execution

1. Record VPS CPU model/vCPU, RAM, disk/storage class, OS, container resource limits and competing services.
2. Record .NET runtime, Kafka/client versions, partition/replica count, producer settings and proxy/TLS path.
3. Start k6 on a separate machine; record its resources and network latency to the VPS.
4. Warm up, then run the gated baseline ladder from 100 to 1,000 req/s.
5. Only proceed to 3,000 and 5,000 req/s after the preceding gates pass.
6. Repeat the ladder at least three times, preserving timestamped summaries.
7. Repeat the highest stable stage for 10 minutes to check sustained capacity and memory growth.
8. Investigate the first failing stage. Increase rates near the boundary to identify a stable limit.

Defaults: 60 seconds per stage, 15 seconds warmup, 10 seconds cooldown, p95 < 200 ms, p99 < 500 ms, HTTP errors < 0.1%, acknowledgements > 99.9%, no dropped iterations. Change these criteria only with an explicit benchmark objective and record the changes.

## Saved evidence

`scripts/run-load-tests.sh` saves each run in `artifacts/load-tests/<UTC timestamp>/`, which is eligible for Git versioning. Each stage has a raw `<rate>.json` summary and a `<rate>.log` terminal transcript. The warmup uses `warmup.json` and `warmup.log`; `run.json` records the endpoint (without URL credentials/query), rates, durations, latency criteria, virtual-user overrides and generator versions. `results.md` compares measured stages. Run metadata uses an explicit list of fields and excludes API keys and other environment secrets.

The runner does not commit or push automatically. Commit a run after it finishes; its directory can contain partial evidence while it is running. Earlier runs retain their original JSON/Markdown files; logs that were not captured at execution time are unavailable. See the [results index](../artifacts/load-tests/README.md) for environment context.

## Interpretation

- `dropped_iterations` with an overloaded generator makes the target rate inconclusive; adjust virtual users or generator capacity and repeat.
- HTTP 429 from the API indicates its concurrency budget was exhausted. With Cloudflare or another proxy in the path, 429 can also come from edge rate limiting; correlate responses with origin logs and edge security events before attributing the failure.
- HTTP 503 indicates an unconfirmed broker operation; timeouts can still have resulted in a Kafka write.
- Broker disk saturation, CPU pressure, API allocation/GC or a proxy bottleneck require distinct experiments.
- Running generator, API and Kafka on one laptop measures a competing-resource environment. It cannot establish VPS capacity.
- Retention and purge may reclaim disk asynchronously. Allow the broker to stabilize before comparisons.

The repository does not contain invented VPS measurements. Local validation, if recorded, is a smoke test only.

For the target VPS, measure the direct origin path first, then repeat with Cloudflare proxying the public hostname. Keep TLS hostname validation and record the path and security rules with each run. See the [project journal and Cloudflare guidance in Portuguese](diario-do-projeto.pt-BR.md).
