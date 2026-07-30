#!/usr/bin/env bash
# Removes scaffold/build debris under repo temp/ (template tests + validate-templates).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TEMP_ROOT="$REPO_ROOT/temp"
WHAT_IF=false
ALL=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --what-if) WHAT_IF=true; shift ;;
    --all) ALL=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--what-if] [--all]"
      echo "  Default: remove temp/MCA_Tests and temp/validate"
      echo "  --all:   remove entire temp/ directory"
      exit 0
      ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

if [[ ! -d "$TEMP_ROOT" ]]; then
  echo "Nothing to clean: $TEMP_ROOT does not exist."
  exit 0
fi

if [[ "$ALL" == true ]]; then
  TARGETS=("$TEMP_ROOT")
else
  TARGETS=("$TEMP_ROOT/MCA_Tests" "$TEMP_ROOT/validate")
fi

echo "Cleaning template/validation temp output under: $TEMP_ROOT"
for target in "${TARGETS[@]}"; do
  if [[ ! -e "$target" ]]; then
    echo "  skip (missing): $target"
    continue
  fi
  size=$(du -sh "$target" 2>/dev/null | cut -f1 || echo "?")
  if [[ "$WHAT_IF" == true ]]; then
    echo "  WHAT-IF: would remove $target ($size)"
    continue
  fi
  echo "  removing $target ($size)..."
  chmod -R u+w "$target" 2>/dev/null || true
  rm -rf "$target"
  echo "  removed."
done

echo "Done."
