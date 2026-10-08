# Saved k6 results

Each directory is named by its run start time in UTC. Completed evidence is versioned here for later analysis and reports. JSON summaries contain the original measured metrics; Markdown reports compare stages.

| Run | Environment | Configuration | Evidence |
|---|---|---|---|
| `20261007T211003Z` | Local API and Kafka on the generator host | 10 seconds per stage, 100–1,000 then 3,000 and 5,000 req/s; no warmup or cooldown | [Results](20261007T211003Z/results.md), [environment details](../../docs/local-smoke-test.md) |
| `20261008T014044Z` | Mac generator → public VPS API at `https://api-kafka.vianadev.com.br` | 15 seconds per stage, 10/50/100 req/s; no warmup, 5-second cooldown; command supplied by the owner | [Results](20261008T014044Z/results.md) |

The second run started on October 7, 2026 at 22:40:44 in São Paulo. All three stages passed, totaling 2,403 confirmed messages, with no HTTP errors or dropped iterations. This is a short smoke test, not a sustained-capacity measurement. The API returns success after Kafka acknowledgement; downstream consumption was not measured.

These historical runs predate automatic terminal-log and parameter capture. Their original JSON/Markdown files are preserved; no terminal transcripts have been reconstructed.

Future executions also save `run.json` with parameters and generator versions, `<rate>.log` with each stage's terminal output, and warmup evidence when enabled. A directory can remain incomplete if a run stops or is still active; only measured stages constitute results. VPS utilization, container limits and competing workloads need separate evidence.
