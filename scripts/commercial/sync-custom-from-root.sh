#!/usr/bin/env bash
# 将根 appsettings.json 同步为定制版模板，并应用交付向叠加项。
# 用法：
#   ./scripts/commercial/sync-custom-from-root.sh           # 同步并写回 configs/定制版.appsettings.json
#   ./scripts/commercial/sync-custom-from-root.sh --dry-run # 只打印将写入的摘要，不落盘
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SRC="$ROOT/appsettings.json"
DST="$ROOT/configs/定制版.appsettings.json"
DRY_RUN=0

for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    -h|--help)
      sed -n '2,6p' "$0"
      exit 0
      ;;
    *)
      echo "未知参数: $arg（支持 --dry-run）" >&2
      exit 1
      ;;
  esac
done

if [[ ! -f "$SRC" ]]; then
  echo "ERROR: 缺少 $SRC" >&2
  exit 1
fi

python3 - "$SRC" "$DST" "$DRY_RUN" <<'PY'
import json, sys
from copy import deepcopy

src_path, dst_path, dry = sys.argv[1], sys.argv[2], sys.argv[3] == "1"

with open(src_path, encoding="utf-8") as f:
    data = json.load(f)

# 交付向叠加：定制版默认带 GUI，便于现场演示；拓扑/点表/Web 等与开发主配置对齐。
sim = data.setdefault("Simulator", {})
runtime = sim.setdefault("Runtime", {})
prev_gui = runtime.get("NoGui")
runtime["NoGui"] = False
sim["Edition"] = {
    "Name": "Custom",
    "LockTopology": False,
    "MaxEssUnits": 0,
    "AllowDroopSlices": True,
    "AllowMainline3d": True,
    "AllowTopologyEditor": True,
}

units = len(data.get("EssUnits") or [])
has_web = "Web" in sim

summary = {
    "EssUnits": units,
    "NoGui_before": prev_gui,
    "NoGui_after": runtime.get("NoGui"),
    "Simulator.Web": "present" if has_web else "absent",
    "top_keys": sorted(data.keys()),
}

print("==> sync-custom-from-root")
print(f"    source: {src_path}")
print(f"    target: {dst_path}")
print(f"    EssUnits: {units}")
print(f"    overlay: Runtime.NoGui {prev_gui!r} -> False")
print(f"    Simulator.Web: {summary['Simulator.Web']}")

if dry:
    print("    dry-run: 未写入文件")
    raise SystemExit(0)

with open(dst_path, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
    f.write("\n")

print("    wrote:", dst_path)
PY

echo "==> 建议接着执行: ./scripts/commercial/check-edition-drift.sh"
