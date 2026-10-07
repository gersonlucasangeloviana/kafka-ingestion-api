"""Produce a comparable report from k6 JSON summaries, without API keys."""
import json
import sys
from pathlib import Path

folder = Path(sys.argv[1])
rows = ["# Load test results", "", "Configured thresholds are a benchmark criterion, not a performance guarantee.", "", "| Target req/s | Confirmed messages | Observed confirmations/s* | p95 ms | p99 ms | HTTP errors % | Ack % | Dropped | Passed |", "|---:|---:|---:|---:|---:|---:|---:|---:|:---:|"]
for path in sorted(folder.glob('[0-9]*.json'), key=lambda path: int(path.stem)):
    metrics = json.loads(path.read_text())['metrics']
    confirmed = metrics.get('confirmed_messages', {}).get('values', {})
    latency = metrics.get('http_req_duration{endpoint:publish}', {}).get('values', {})
    dropped = metrics.get('dropped_iterations', {}).get('values', {}).get('count', 0)
    thresholds = [t for metric in metrics.values() for t in metric.get('thresholds', {}).values()]
    passed = bool(thresholds) and all(t['ok'] for t in thresholds)
    errors = metrics.get('http_req_failed{endpoint:publish}', {}).get('values', {}).get('rate', 0) * 100
    acknowledgements = metrics.get('kafka_confirmations', {}).get('values', {}).get('rate', 0) * 100
    rows.append(f"| {path.stem} | {confirmed.get('count', 0)} | {confirmed.get('rate', 0):.1f} | {latency.get('p(95)', 0):.2f} | {latency.get('p(99)', 0):.2f} | {errors:.3f} | {acknowledgements:.3f} | {dropped} | {'Yes' if passed else 'No'} |")
rows.extend(['', '*Counter rate includes setup and graceful completion time. Compare counts and dropped iterations with the configured measurement duration.', ''])
report = folder / 'results.md'
report.write_text('\n'.join(rows))
print(f'Report: {report}')
