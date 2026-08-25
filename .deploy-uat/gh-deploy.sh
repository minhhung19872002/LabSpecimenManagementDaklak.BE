#!/usr/bin/env bash
# /opt/starlab/gh-deploy.sh — script deploy cho GitHub Actions
#
# Được gọi qua forced command trong authorized_keys, nên key này KHÔNG mở được shell.
# Actions gọi:  echo "$GHCR_TOKEN" | ssh -i key hung@14.225.83.93 backend
#   - service lấy từ SSH_ORIGINAL_COMMAND (backend | frontend)
#   - token GHCR đọc từ stdin (không nằm trên command line -> không lộ trong ps/log)
#
# Chuyen sang VM 14.225.83.93 ngay 2026-08-25 (4 vCPU/11GB, dung chung 5 he).
# Van chi pull, khong build: giu nhat quan va khong tranh CPU voi cac he khac.
# KHONG dung sudo — user hung thuoc nhom docker; VM nay sudo co mat khau.
set -euo pipefail

DEPLOY_DIR=/home/hung/apps/starlab
GHCR_USER=minhhung19872002
LOG=/home/hung/apps/starlab/deploy.log

log() { echo "[$(date -u +%FT%TZ)] $*" | tee -a "$LOG"; }

SERVICE="${SSH_ORIGINAL_COMMAND:-}"
case "$SERVICE" in
backend | frontend) ;;
*)
    echo "usage: ssh ... backend|frontend (got: '$SERVICE')" >&2
    exit 2
    ;;
esac

log "deploy $SERVICE bat dau"

# Token GHCR từ stdin (timeout 10s để không treo nếu Actions không gửi)
if ! read -r -t 10 GHCR_TOKEN || [ -z "${GHCR_TOKEN:-}" ]; then
    log "LOI: khong nhan duoc GHCR token qua stdin"
    exit 3
fi

echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin >/dev/null
log "da dang nhap ghcr.io"

cd "$DEPLOY_DIR"

if [ "$SERVICE" = backend ]; then
    log "pull image backend + migrator"
    docker compose pull starlab-backend starlab-db-migrator

    log "chay db migration"
    # up (khong phai run) de dung dung config trong compose; container exit 0 la xong
    docker compose up --exit-code-from starlab-db-migrator starlab-db-migrator || {
        log "LOI: migration that bai — KHONG deploy backend"
        exit 4
    }
    log "migration xong"

    docker compose up -d --no-deps starlab-backend
else
    log "pull image frontend"
    docker compose pull starlab-frontend
    docker compose up -d --no-deps starlab-frontend
fi

# Đợi healthy — máy 1 vCPU nên .NET khởi động chậm, cho tối đa 3 phút
CONTAINER="lab-specimen-$SERVICE"
for i in $(seq 1 36); do
    s=$(docker inspect "$CONTAINER" --format '{{.State.Health.Status}}' 2>/dev/null || echo none)
    if [ "$s" = healthy ]; then
        log "deploy $SERVICE THANH CONG (healthy sau $((i * 5))s)"
        docker image prune -f >/dev/null 2>&1 || true
        exit 0
    fi
    sleep 5
done

log "LOI: $CONTAINER khong healthy sau 180s"
docker logs "$CONTAINER" --tail 30 2>&1 | tee -a "$LOG"
exit 5
