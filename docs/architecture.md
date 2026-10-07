# Architecture decisions

## Minimal API with feature folders

This service has two use cases and one external dependency. ASP.NET Core Minimal APIs, typed results, native authentication/authorization, validated options, health checks and Problem Details provide the necessary structure. Feature folders keep the HTTP contracts discoverable. A multi-project domain/application/infrastructure split would add indirection without a domain model that needs it.

## Acknowledgement before HTTP success

The application awaits `ProduceAsync` rather than reporting success after enqueueing to an in-memory buffer. Shared producer instances and Kafka batching preserve throughput across concurrent requests. Producer idempotence is enabled, but client-request deduplication is explicitly outside the contract.

## Destructive operations have a different authorization policy

Administrative credentials are distinct from publication credentials. Deletion is disabled by default, restricted to the configured topic, requires a confirmation header and admits one operation per instance. There is no endpoint that accepts arbitrary topic names. Hashes of credentials are compared in constant time; credentials and message payloads are never intentionally logged.

## Delete records, preserve topic

Capturing numeric end offsets then calling DeleteRecords preserves partitions and configuration. A snapshot is per partition, not a cross-partition transaction. Concurrent writes after a partition snapshot survive; failures may leave a partial deletion. The API does not promise an empty topic while producers are active or consumer-group resets.

## Bounded backpressure

HTTP body limits, a configurable concurrency limiter, delivery timeout and bounded producer buffers prevent unlimited buffering. Rejection is visible to the benchmark. There is no requests-per-second limiter that would impose an artificial throughput ceiling.

## Reproducible performance evidence

k6 uses an open workload with constant arrival rate and exactly one publication per iteration. Every stage must pass acknowledgement, error, latency and dropped-iteration thresholds before the runner advances. Stored JSON summaries allow later comparison; machine capacity is measured rather than inferred from configured target rate.
