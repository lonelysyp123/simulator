#!/usr/bin/env bash
# 可选：校验 pointmaps/models 完整性。开发与运行时不再把点表同步到仓库根。
# 发布请用 copy_pointmaps_to（publish-*.sh 已调用），只把 models 目录带到输出。
# 用法:
#   ./scripts/sync-pointmaps-to-root.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$(dirname "$0")/pointmap-common.sh"

validate_device_models

echo "OK. Runtime point maps live in pointmaps/models/ (no root CSV copy needed)."
echo "Publish output still gets a full models tree via copy_pointmaps_to."
