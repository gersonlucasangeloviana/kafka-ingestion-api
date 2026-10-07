#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
set -a
source .env.local
set +a
export Kafka__BootstrapServers="${Kafka__BootstrapServers:-localhost:19092}"
exec dotnet run --project src/KafkaIngestion.Api -c Release --no-launch-profile --urls "http://localhost:${API_PORT:-8080}"
