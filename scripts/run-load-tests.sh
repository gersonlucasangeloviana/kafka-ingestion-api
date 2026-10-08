#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ -f .env.local ]]; then
  set -a
  source .env.local
  set +a
fi
export API_KEY="${API_KEY:-${Authentication__ApiKey:-}}"
: "${API_KEY:?Set API_KEY or create .env.local}"
export BASE_URL="${BASE_URL:-http://localhost:8080}"
export DURATION="${DURATION:-60s}"
export RATES="${RATES:-100 200 300 400 500 600 700 800 900 1000 3000 5000}"
export WARMUP="${WARMUP:-true}"
export COOLDOWN_SECONDS="${COOLDOWN_SECONDS:-10}"
export RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
results="artifacts/load-tests/$RUN_ID"
mkdir -p "$results"
python3 - "$results" <<'PY'
import json
import os
import platform
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

url = urlsplit(os.environ["BASE_URL"])
host = url.hostname or ""
if ":" in host:
    host = f"[{host}]"
if url.port:
    host += f":{url.port}"
metadata = {
    "run_id": os.environ["RUN_ID"],
    "base_url": urlunsplit((url.scheme, host, url.path, "", "")),
    "rates": os.environ["RATES"].split(),
    "duration_per_stage": os.environ["DURATION"],
    "warmup": os.environ["WARMUP"] == "true",
    "warmup_duration": "15s",
    "warmup_rps": 100,
    "cooldown_seconds": os.environ["COOLDOWN_SECONDS"],
    "p95_limit_ms": os.environ.get("P95_MS", "200"),
    "p99_limit_ms": os.environ.get("P99_MS", "500"),
    "pre_allocated_vus": os.environ.get("PRE_ALLOCATED_VUS", "automatic per rate"),
    "max_vus": os.environ.get("MAX_VUS", "automatic per rate"),
    "generator_os": platform.platform(),
    "k6_version": subprocess.check_output(["k6", "version"], text=True).strip(),
}
(Path(sys.argv[1]) / "run.json").write_text(json.dumps(metadata, indent=2) + "\n")
PY
echo "Saving results and terminal logs to $results"
if [[ "$WARMUP" == "true" ]]; then
  RPS=100 DURATION=15s SUMMARY_PATH="$results/warmup.json" k6 run load-tests/ingestion.js 2>&1 | tee "$results/warmup.log"
fi
for rate in $RATES; do
  export RPS="$rate"
  export SUMMARY_PATH="$results/$rate.json"
  if ! k6 run load-tests/ingestion.js 2>&1 | tee "$results/$rate.log"; then
    python3 scripts/summarize-load-tests.py "$results"
    echo "Stopped at $rate req/s because a threshold failed. See $results."
    exit 1
  fi
  # No purge between stages: deletion I/O would distort the next measurement.
  if [[ "$COOLDOWN_SECONDS" != "0" ]]; then
    sleep "$COOLDOWN_SECONDS"
  fi
done
python3 scripts/summarize-load-tests.py "$results"
