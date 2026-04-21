#!/usr/bin/env bash
set -euo pipefail

SERVICE="${SSH_ORIGINAL_COMMAND:-}"
case "$SERVICE" in
  backend)  REPO_DIR="$HOME/starlab/lab-specimen-management-be" ;;
  frontend) REPO_DIR="$HOME/starlab/lab-specimen-management-fe" ;;
  *) echo "usage: ssh ... backend|frontend (got: '$SERVICE')" >&2; exit 2 ;;
esac

echo "[$(date -u +%FT%TZ)] deploy $SERVICE start"
cd "$REPO_DIR"
git fetch --prune origin develop
git reset --hard origin/develop
cd "$HOME/starlab"
docker compose up -d --build --no-deps "$SERVICE"
echo "[$(date -u +%FT%TZ)] deploy $SERVICE done"
