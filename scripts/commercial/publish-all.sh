#!/usr/bin/env bash
# 为指定版本或全部版本发布 Windows + Linux 包
# 用法:
#   ./scripts/commercial/publish-all.sh              # 三个版本 × win + linux
#   ./scripts/commercial/publish-all.sh 社区版     # 仅社区版
#   EDITION=充值版 ./scripts/commercial/publish-all.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=publish-common.sh
source "$(dirname "$0")/publish-common.sh"

TARGET="${1:-all}"

editions_to_publish() {
  if [[ "$TARGET" == "all" ]]; then
    printf '%s\n' "${ALL_EDITIONS[@]}"
  else
    validate_edition "$TARGET"
    echo "$TARGET"
  fi
}

ensure_dist_layout

while IFS= read -r edition; do
  echo ""
  echo "========================================"
  echo "  版本: $edition"
  echo "========================================"
  EDITION="$edition" "$ROOT/scripts/commercial/publish-windows.sh"
  EDITION="$edition" "$ROOT/scripts/commercial/publish-linux.sh"
done < <(editions_to_publish)

echo ""
echo "dist/ 布局:"
find "$ROOT/dist" -maxdepth 2 -type d | sort
