#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="mca-api"
TAG="local"
REGISTRY=""
DOCKERFILE="Dockerfile"
PUSH="false"
NO_BUILD="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --image-name)
      IMAGE_NAME="$2"
      shift 2
      ;;
    --tag)
      TAG="$2"
      shift 2
      ;;
    --registry)
      REGISTRY="$2"
      shift 2
      ;;
    --dockerfile)
      DOCKERFILE="$2"
      shift 2
      ;;
    --push)
      PUSH="true"
      shift
      ;;
    --no-build)
      NO_BUILD="true"
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOCKERFILE_PATH="$PROJECT_ROOT/$DOCKERFILE"

if [[ ! -f "$DOCKERFILE_PATH" ]]; then
  echo "Dockerfile not found: $DOCKERFILE_PATH" >&2
  exit 1
fi

if [[ -z "$REGISTRY" ]]; then
  FULL_IMAGE_NAME="$IMAGE_NAME:$TAG"
else
  REGISTRY="${REGISTRY%/}"
  FULL_IMAGE_NAME="$REGISTRY/$IMAGE_NAME:$TAG"
fi

if [[ "$NO_BUILD" != "true" ]]; then
  docker build -f "$DOCKERFILE_PATH" -t "$FULL_IMAGE_NAME" "$PROJECT_ROOT"
fi

if [[ "$PUSH" == "true" ]]; then
  docker push "$FULL_IMAGE_NAME"
fi

echo "Image ready: $FULL_IMAGE_NAME"
