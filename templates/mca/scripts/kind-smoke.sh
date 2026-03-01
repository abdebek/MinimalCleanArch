#!/usr/bin/env bash
set -euo pipefail

IMAGE_TAG="mca-api:local"
NAMESPACE="mca"
CLUSTER_NAME="mca-local"
DEPLOYMENT_NAME="mca-api"
LOCAL_PORT="5080"
SKIP_BUILD="false"
SKIP_SMOKE="false"
TIMEOUT_SECONDS="180"

while [[ $# -gt 0 ]]; do
  case "$1" in
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
    --deployment-name)
      DEPLOYMENT_NAME="$2"
      shift 2
      ;;
    --local-port)
      LOCAL_PORT="$2"
      shift 2
      ;;
    --skip-build)
      SKIP_BUILD="true"
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
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

for cmd in docker kind kubectl; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Required command '$cmd' is not available." >&2
    exit 1
  fi
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [[ "$SKIP_BUILD" != "true" ]]; then
  docker build -t "$IMAGE_TAG" "$PROJECT_ROOT"
fi

if ! kind get clusters | grep -qx "$CLUSTER_NAME"; then
  kind create cluster --name "$CLUSTER_NAME"
fi

kind load docker-image "$IMAGE_TAG" --name "$CLUSTER_NAME"

kubectl apply -f - <<EOF
apiVersion: v1
kind: Namespace
metadata:
  name: $NAMESPACE
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: $DEPLOYMENT_NAME
  namespace: $NAMESPACE
spec:
  replicas: 1
  selector:
    matchLabels:
      app: $DEPLOYMENT_NAME
  template:
    metadata:
      labels:
        app: $DEPLOYMENT_NAME
    spec:
      containers:
      - name: api
        image: $IMAGE_TAG
        imagePullPolicy: IfNotPresent
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Development
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: $DEPLOYMENT_NAME
  namespace: $NAMESPACE
spec:
  selector:
    app: $DEPLOYMENT_NAME
  ports:
  - name: http
    port: 80
    targetPort: 8080
EOF

kubectl -n "$NAMESPACE" rollout status "deployment/$DEPLOYMENT_NAME" --timeout="${TIMEOUT_SECONDS}s"

if [[ "$SKIP_SMOKE" == "true" ]]; then
  echo "Kubernetes deployment completed (smoke test skipped)."
  exit 0
fi

kubectl -n "$NAMESPACE" port-forward "service/$DEPLOYMENT_NAME" "${LOCAL_PORT}:80" >/dev/null 2>&1 &
PORT_FORWARD_PID=$!
cleanup() {
  kill "$PORT_FORWARD_PID" >/dev/null 2>&1 || true
}
trap cleanup EXIT

sleep 3
"$SCRIPT_DIR/smoke-test.sh" --base-url "http://localhost:$LOCAL_PORT" --path "/api/todos" --timeout-seconds "$TIMEOUT_SECONDS"
echo "Kind smoke deployment completed."
