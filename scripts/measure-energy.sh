#!/usr/bin/env bash
# Measure RAPL joules for one explicitly supplied workload. Compare baseline and patched
# versions on the same host with the same workload. No values are pre-filled.
set -euo pipefail
if [ "$#" -lt 2 ]; then
  echo "Usage: $0 BASELINE_OR_PATCHED command [arguments...]" >&2
  echo "Example: $0 patched bash -c 'sleep 3600'" >&2
  exit 2
fi
label=$1; shift
energy_file=${RAPL_ENERGY_FILE:-}
if [ -z "$energy_file" ]; then
  energy_file=$(find /sys/class/powercap -name energy_uj -type f -print -quit 2>/dev/null || true)
fi
if [ -z "$energy_file" ] || [ ! -r "$energy_file" ]; then
  echo "No readable RAPL energy_uj device. Use an external wall-power meter instead." >&2
  exit 2
fi
before=$(cat "$energy_file")
before_time=$(date +%s%N)
"$@"
after_time=$(date +%s%N)
after=$(cat "$energy_file")
# Account for counter wrap; device max range is reported in microjoules.
if [ "$after" -lt "$before" ]; then
  range=$(cat "$(dirname "$energy_file")/max_energy_range_uj")
  delta=$((range - before + after))
else
  delta=$((after - before))
fi
python3 - "$label" "$delta" "$before_time" "$after_time" <<'PY'
import json,sys
label, microjoules, start, end = sys.argv[1:]
print(json.dumps({"variant":label,"joules":int(microjoules)/1e6,
                  "elapsed_seconds":(int(end)-int(start))/1e9},indent=2))
PY
