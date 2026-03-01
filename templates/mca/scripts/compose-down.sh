#!/usr/bin/env bash
set -euo pipefail

REMOVE_VOLUMES="false"
COMPOSE_FILE="docker-compose.yml"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --remove-volumes)
      REMOVE_VOLUMES="true"
      shift
      ;;
    --compose-file)
      COMPOSE_FILE="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMPOSE_PATH="$PROJECT_ROOT/$COMPOSE_FILE"

if [[ ! -f "$COMPOSE_PATH" ]]; then
  echo "Compose file not found: $COMPOSE_PATH" >&2
  exit 1
fi

DEFAULT_PROJECT_NAME="$(basename "$PROJECT_ROOT" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9_-]+/-/g; s/^-+//; s/-+$//')"
if [[ -z "$DEFAULT_PROJECT_NAME" ]]; then
  DEFAULT_PROJECT_NAME="mca"
fi
if [[ ! "$DEFAULT_PROJECT_NAME" =~ ^[a-z0-9] ]]; then
  DEFAULT_PROJECT_NAME="mca-$DEFAULT_PROJECT_NAME"
fi
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-$DEFAULT_PROJECT_NAME}"

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose)
else
  echo "Docker Compose not found. Install Docker Desktop or docker-compose." >&2
  exit 1
fi

ARGS=(-f "$COMPOSE_PATH" down --remove-orphans)
if [[ "$REMOVE_VOLUMES" == "true" ]]; then
  ARGS+=(-v)
fi

"${COMPOSE_CMD[@]}" "${ARGS[@]}"
echo "Compose services stopped."
