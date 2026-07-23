#!/usr/bin/env bash
set -Eeuo pipefail

PROJECT_DIR="/opt/fixar/fixar-digital-factory"
COMPOSE_FILE="docker-compose.production.yml"
ENV_FILE=".env.production"
BRANCH="fix/live-production-integration"

cd "$PROJECT_DIR"

echo "======================================"
echo " Fixar OS Production Deployment"
echo "======================================"

if [ ! -f "$ENV_FILE" ]; then
    echo "HATA: $ENV_FILE bulunamadı."
    exit 1
fi

if ! git diff --quiet || ! git diff --cached --quiet; then
    echo "HATA: Commit edilmemiş kod değişikliği var."
    echo "Önce değişiklikleri commit edin veya stash yapın."
    git status --short
    exit 1
fi

echo
echo "1/4 - GitHub bağlantısı kontrol ediliyor..."
git fetch origin "$BRANCH"

echo
echo "2/4 - Son kodlar indiriliyor..."
git checkout "$BRANCH"
git pull --ff-only origin "$BRANCH"

echo
echo "3/4 - Docker servisleri build edilip başlatılıyor..."
docker compose \
  --env-file "$ENV_FILE" \
  -f "$COMPOSE_FILE" \
  up -d --build --remove-orphans

echo
echo "4/4 - Servislerin durumu kontrol ediliyor..."
docker compose \
  --env-file "$ENV_FILE" \
  -f "$COMPOSE_FILE" \
  ps

echo
echo "======================================"
echo " Fixar OS deployment tamamlandı."
echo "======================================"
