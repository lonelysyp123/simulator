#!/usr/bin/env bash
# 点位表版本管理与复制（供发布脚本、sync-pointmaps-to-root.sh 引用）
set -euo pipefail

_POINTMAP_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
POINTMAP_ROOT="${POINTMAP_ROOT:-$(cd "$_POINTMAP_SCRIPT_DIR/.." && pwd)/pointmaps}"

# 商业发布固定使用 common
DEFAULT_COMMERCIAL_POINTMAP_VERSION="common"
DEFAULT_DEV_POINTMAP_VERSION="common"

POINTMAP_VERSION_IDS=(
  "common"
  "lc"
  "battery"
)

POINTMAP_RUNTIME_FILES=(
  emu.csv
  em.csv
  bms_bank.csv
  bms_rack.csv
  lc.csv
)

pointmap_version_dir() {
  local version="$1"
  echo "$POINTMAP_ROOT/$version"
}

validate_pointmap_version() {
  local version="$1"
  local id
  for id in "${POINTMAP_VERSION_IDS[@]}"; do
    if [[ "$id" == "$version" ]]; then
      local dir
      dir="$(pointmap_version_dir "$version")"
      if [[ ! -d "$dir" ]]; then
        echo "点位表版本目录不存在: $dir" >&2
        return 1
      fi
      local f
      for f in "${POINTMAP_RUNTIME_FILES[@]}"; do
        if [[ ! -f "$dir/$f" ]]; then
          echo "点位表版本 [$version] 缺少文件: $f" >&2
          return 1
        fi
      done
      return 0
    fi
  done
  echo "未知点位表版本: $version（可选: ${POINTMAP_VERSION_IDS[*]}）" >&2
  return 1
}

copy_pointmaps_to() {
  local out="$1"
  local version="$2"

  validate_pointmap_version "$version"

  local src
  src="$(pointmap_version_dir "$version")"
  echo "==> Copying point maps [$version] from $src ..."
  local f
  for f in "${POINTMAP_RUNTIME_FILES[@]}"; do
    cp -f "$src/$f" "$out/$f"
    echo "    $f"
  done
  if [[ -f "$src/version.json" ]]; then
    cp -f "$src/version.json" "$out/pointmap-version.json"
    echo "    pointmap-version.json (from version.json)"
  fi

  copy_device_models_to "$out"
}

# 设备型号点表（pointmaps/models/{设备类型}/{型号}/）：整目录随发布携带，
# 运行期可在系统配置界面切换选型（configs/topology/device-models.json）。
copy_device_models_to() {
  local out="$1"
  local models_src="$POINTMAP_ROOT/models"
  if [[ ! -d "$models_src" ]]; then
    echo "==> Skip device models (not found: $models_src)"
    return 0
  fi
  echo "==> Copying device model point maps from $models_src ..."
  rm -rf "$out/pointmaps/models"
  mkdir -p "$out/pointmaps"
  cp -R "$models_src" "$out/pointmaps/models"
}

list_pointmap_versions() {
  local id
  for id in "${POINTMAP_VERSION_IDS[@]}"; do
    echo "$id"
  done
}
