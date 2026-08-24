#!/usr/bin/env bash
# 将 standard 型号点位表同步到仓库根目录（供 dotnet run / 本地联调）
# 用法:
#   ./scripts/sync-pointmaps-to-root.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$(dirname "$0")/pointmap-common.sh"

validate_device_models
copy_pointmaps_to "$ROOT"

echo "Done. Root directory now uses model: $DEFAULT_ROOT_MODEL"
