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
export RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
results="artifacts/load-tests/$RUN_ID"
mkdir -p "$results"
if [[ "${WARMUP:-true}" == "true" ]]; then
  RPS=100 DURATION=15s SUMMARY_PATH="$results/warmup.json" k6 run load-tests/ingestion.js
fi
for rate in ${RATES:-100 200 300 400 500 600 700 800 900 1000 3000 5000}; do
  export RPS="$rate"
  export SUMMARY_PATH="$results/$rate.json"
  if ! k6 run load-tests/ingestion.js; then
    python3 scripts/summarize-load-tests.py "$results"
    echo "Stopped at $rate req/s because a threshold failed. See $results."
    exit 1
  fi
  # No purge between stages: deletion I/O would distort the next measurement.
  if [[ "${COOLDOWN_SECONDS:-10}" != "0" ]]; then
    sleep "${COOLDOWN_SECONDS:-10}"
  fi
done
python3 scripts/summarize-load-tests.py "$results"
