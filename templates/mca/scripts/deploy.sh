#!/usr/bin/env bash
set -euo pipefail

TARGET="compose"
NO_BUILD="false"
SKIP_SMOKE="false"
TIMEOUT_SECONDS="180"
BASE_URL="http://localhost:5000"
IMAGE_TAG="mca-api:local"
NAMESPACE="mca"
CLUSTER_NAME="mca-local"
KIND_LOCAL_PORT="5080"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --target)
      TARGET="$2"
      shift 2
      ;;
    --no-build)
      NO_BUILD="true"
      shift
      ;;
    --skip-smoke-test)
      SKIP_SMOKE="true"
      shift
      ;;
    --timeout-seconds)
      TIMEOUT_SECONDS="$2"
      shift 2
      ;;
    --base-url)
      BASE_URL="$2"
      shift 2
      ;;
    --image-tag)
      IMAGE_TAG="$2"
      shift 2
      ;;
    --namespace)
      NAMESPACE="$2"
      shift 2
      ;;
    --cluster-name)
      CLUSTER_NAME="$2"
      shift 2
      ;;
    --kind-local-port)
      KIND_LOCAL_PORT="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ "$TARGET" == "compose" ]]; then
  ARGS=()
  [[ "$NO_BUILD" == "true" ]] && ARGS+=(--no-build)
  [[ "$SKIP_SMOKE" == "true" ]] && ARGS+=(--skip-smoke-test)
  ARGS+=(--timeout-seconds "$TIMEOUT_SECONDS" --base-url "$BASE_URL")
  "$SCRIPT_DIR/compose-up.sh" "${ARGS[@]}"
elif [[ "$TARGET" == "kind" ]]; then
  ARGS=(--image-tag "$IMAGE_TAG" --namespace "$NAMESPACE" --cluster-name "$CLUSTER_NAME" --local-port "$KIND_LOCAL_PORT" --timeout-seconds "$TIMEOUT_SECONDS")
  [[ "$NO_BUILD" == "true" ]] && ARGS+=(--skip-build)
  [[ "$SKIP_SMOKE" == "true" ]] && ARGS+=(--skip-smoke-test)
  "$SCRIPT_DIR/kind-smoke.sh" "${ARGS[@]}"
else
  echo "Invalid target '$TARGET'. Use 'compose' or 'kind'." >&2
  exit 1
fi
