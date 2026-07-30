#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5000"
PATH_VALUE="/api/todos"
TIMEOUT_SECONDS="180"
POLL_INTERVAL_SECONDS="2"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-url)
      BASE_URL="$2"
      shift 2
      ;;
    --path)
      PATH_VALUE="$2"
      shift 2
      ;;
    --timeout-seconds)
      TIMEOUT_SECONDS="$2"
      shift 2
      ;;
    --poll-interval-seconds)
      POLL_INTERVAL_SECONDS="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required for smoke tests." >&2
  exit 1
fi

BASE_URL="${BASE_URL%/}"
if [[ "$PATH_VALUE" != /* ]]; then
  PATH_VALUE="/$PATH_VALUE"
fi
URL="$BASE_URL$PATH_VALUE"

DEADLINE=$(( $(date +%s) + TIMEOUT_SECONDS ))
LAST_ERROR=""

while [[ $(date +%s) -lt $DEADLINE ]]; do
  STATUS_CODE="$(curl -k -s -o /dev/null -w '%{http_code}' --max-redirs 0 "$URL" || true)"
  if [[ -n "$STATUS_CODE" && "$STATUS_CODE" -ge 200 && "$STATUS_CODE" -lt 500 ]]; then
    echo "Smoke test succeeded: $URL returned HTTP $STATUS_CODE."
    exit 0
  fi

  LAST_ERROR="HTTP ${STATUS_CODE:-000}"
  sleep "$POLL_INTERVAL_SECONDS"
done

echo "Smoke test failed for $URL within ${TIMEOUT_SECONDS}s. Last error: $LAST_ERROR" >&2
exit 1
