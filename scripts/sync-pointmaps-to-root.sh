#!/usr/bin/env bash
# 将指定版本的点位表同步到仓库根目录（供 dotnet run / 本地联调）
# 用法:
#   ./scripts/sync-pointmaps-to-root.sh           # 默认 common
#   ./scripts/sync-pointmaps-to-root.sh lc
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$(dirname "$0")/pointmap-common.sh"

VERSION="${1:-${POINTMAP_VERSION:-$DEFAULT_DEV_POINTMAP_VERSION}}"

validate_pointmap_version "$VERSION"
copy_pointmaps_to "$ROOT" "$VERSION"

echo "Done. Root directory now uses point map version: $VERSION"
