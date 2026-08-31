#!/usr/bin/env bash
# 点位表管理与复制（供发布脚本、sync-pointmaps-to-root.sh 引用）
# 运行时只从 pointmaps/models/{设备类型}/{型号}/ 解析，不再把 CSV 摊到目标根目录。
# 注意：仅使用 bash 3.2 兼容语法（macOS 自带 bash 为 3.2，不支持 declare -A）。
set -euo pipefail

_POINTMAP_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
POINTMAP_ROOT="${POINTMAP_ROOT:-$(cd "$_POINTMAP_SCRIPT_DIR/.." && pwd)/pointmaps}"

DEFAULT_ROOT_MODEL="standard"

# 设备类型 → 型号目录内必须存在的点表文件（每行 "类型 文件1 [文件2 ...]"）
DEVICE_TYPE_FILES="
bms bms_bank.csv bms_rack.csv
emu emu.csv
em em.csv
lc lc.csv
pv pv_logger.csv pv_apm810.csv
"

device_model_dir() {
  local type="$1"
  local model="${2:-$DEFAULT_ROOT_MODEL}"
  echo "$POINTMAP_ROOT/models/$type/$model"
}

validate_device_models() {
  local type files dir f
  while read -r type files; do
    [[ -z "$type" ]] && continue
    dir="$(device_model_dir "$type")"
    if [[ ! -d "$dir" ]]; then
      echo "设备型号点表目录不存在: $dir" >&2
      return 1
    fi
    for f in $files; do
      if [[ ! -f "$dir/$f" ]]; then
        echo "设备型号点表 [$type/$DEFAULT_ROOT_MODEL] 缺少文件: $f" >&2
        return 1
      fi
    done
  done <<< "$DEVICE_TYPE_FILES"
  return 0
}

# 将 pointmaps/models 复制到目标目录（发布输出）。不再摊平 CSV 到目标根。
# 第二个参数（旧点位表版本名）已废弃，仅为兼容旧调用保留。
copy_pointmaps_to() {
  local out="$1"
  if [[ $# -ge 2 && "$2" != "$DEFAULT_ROOT_MODEL" ]]; then
    echo "提示: 点位表版本参数 [$2] 已废弃，运行时从 pointmaps/models 按选型解析" >&2
  fi

  validate_device_models
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

list_device_types() {
  local type files
  while read -r type files; do
    [[ -z "$type" ]] && continue
    echo "$type"
  done <<< "$DEVICE_TYPE_FILES"
}
