#!/usr/bin/env bash
# 仅同步配置/点表/文档到已发布的 dist 目录（不重新编译）
# 用法:
#   ./scripts/commercial/sync-runtime.sh              # 三个版本 × win + linux
#   ./scripts/commercial/sync-runtime.sh 社区版 win-x64
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=publish-common.sh
source "$(dirname "$0")/publish-common.sh"

EDITION_ARG="${1:-all}"
RID_ARG="${2:-all}"

sync_one() {
  local edition="$1"
  local rid="$2"
  local out
  out="$(dist_out_dir "$edition" "$rid")"

  if [[ ! -d "$out" ]]; then
    echo "跳过（目录不存在）: $out"
    return 0
  fi

  local platform="linux"
  if [[ "$rid" == win-* ]]; then
    platform="windows"
  fi

  echo "==> Sync $edition / $rid -> $out"
  copy_runtime_files "$out" "$edition"
  copy_platform_files "$out" "$edition" "$platform"
}

ensure_dist_layout

if [[ "$EDITION_ARG" == "all" ]]; then
  for edition in "${ALL_EDITIONS[@]}"; do
    for rid in win-x64 linux-arm64; do
      sync_one "$edition" "$rid"
    done
  done
else
  validate_edition "$EDITION_ARG"
  if [[ "$RID_ARG" == "all" ]]; then
    for rid in win-x64 linux-arm64; do
      sync_one "$EDITION_ARG" "$rid"
    done
  else
    sync_one "$EDITION_ARG" "$RID_ARG"
  fi
fi

echo "Done."
