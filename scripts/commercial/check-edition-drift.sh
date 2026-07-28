#!/usr/bin/env bash
# 商业档位配置差异检查（发布前建议执行）
# 主开关：Simulator.Edition.Name = Community | Commercial | Custom
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMMUNITY="$ROOT/configs/社区版.appsettings.json"
COMMERCIAL="$ROOT/configs/商业版.appsettings.json"
CUSTOM="$ROOT/configs/定制版.appsettings.json"
ROOT_CFG="$ROOT/appsettings.json"

fail=0
warn=0

json_get() {
  local file="$1"
  local path="$2"
  python3 - "$file" "$path" <<'PY'
import json, sys
path = sys.argv[2].split(".")
with open(sys.argv[1], encoding="utf-8") as f:
    data = json.load(f)
cur = data
for p in path:
    if cur is None:
        print("")
        raise SystemExit(0)
    if isinstance(cur, list):
        cur = cur[int(p)]
    else:
        cur = cur.get(p) if isinstance(cur, dict) else None
if cur is None:
    print("")
elif isinstance(cur, list):
    print(len(cur))
elif isinstance(cur, bool):
    print("true" if cur else "false")
else:
    print(cur)
PY
}

echo "==> 检查商业档位配置差异（Edition 开关）"
if [[ ! -f "$COMMUNITY" || ! -f "$COMMERCIAL" || ! -f "$CUSTOM" ]]; then
  echo "ERROR: configs/ 下缺少社区版/商业版/定制版模板" >&2
  exit 1
fi

c_name="$(json_get "$COMMUNITY" "Simulator.Edition.Name")"
m_name="$(json_get "$COMMERCIAL" "Simulator.Edition.Name")"
c_droop="$(json_get "$COMMUNITY" "Simulator.Edition.AllowDroopSlices")"
m_droop="$(json_get "$COMMERCIAL" "Simulator.Edition.AllowDroopSlices")"
c_lock="$(json_get "$COMMUNITY" "Simulator.Edition.LockTopology")"
c_max="$(json_get "$COMMUNITY" "Simulator.Edition.MaxEssUnits")"
c_units="$(json_get "$COMMUNITY" "EssUnits")"
m_units="$(json_get "$COMMERCIAL" "EssUnits")"
custom_units="$(json_get "$CUSTOM" "EssUnits")"
root_units="$(json_get "$ROOT_CFG" "EssUnits")"
c_bind="$(json_get "$COMMUNITY" "Simulator.Protocol.BindAddress")"

echo "    社区版 Edition=$c_name AllowDroop=$c_droop Lock=$c_lock MaxUnits=$c_max Units=$c_units Bind=$c_bind"
echo "    商业版 Edition=$m_name AllowDroop=$m_droop Units=$m_units"
echo "    定制版 Units=$custom_units  |  根 appsettings Units=$root_units"

if [[ "$c_name" != "Community" && "$c_name" != "社区版" ]]; then
  echo "ERROR: 社区版 Simulator.Edition.Name 应为 Community，当前=$c_name" >&2
  fail=1
fi
if [[ "$c_droop" != "false" ]]; then
  echo "ERROR: 社区版 AllowDroopSlices 应为 false（高级 API 关闭），当前=$c_droop" >&2
  fail=1
fi
if [[ "$c_lock" != "true" ]]; then
  echo "ERROR: 社区版 LockTopology 应为 true，当前=$c_lock" >&2
  fail=1
fi
if [[ -n "$c_max" && "$c_max" != "0" && "$c_max" != "2" ]]; then
  echo "WARN: 社区版 MaxEssUnits 建议为 2，当前=$c_max" >&2
  warn=1
fi
if [[ "$c_bind" != "127.0.0.1" && -n "$c_bind" ]]; then
  echo "WARN: 社区版 BindAddress 建议 127.0.0.1，当前=$c_bind" >&2
  warn=1
fi

if [[ "$m_name" != "Commercial" && "$m_name" != "商业版" ]]; then
  echo "ERROR: 商业版 Simulator.Edition.Name 应为 Commercial，当前=$m_name" >&2
  fail=1
fi
if [[ "$m_droop" != "true" ]]; then
  echo "ERROR: 商业版 AllowDroopSlices 应为 true，当前=$m_droop" >&2
  fail=1
fi

# 商业版应对齐开发主配置规模（完整能力）
if [[ "$m_units" != "$root_units" ]]; then
  echo "WARN: 商业版与根 appsettings 的 EssUnits 不一致（$m_units vs $root_units），可用 sync-custom 思路同步商业版模板" >&2
  warn=1
fi

if [[ "$custom_units" != "$root_units" ]]; then
  echo "WARN: 定制版与根 appsettings.json 的 EssUnits 单元数不一致（$custom_units vs $root_units）" >&2
  warn=1
fi

if [[ "$fail" -ne 0 ]]; then
  echo "==> 检查失败（见上方 ERROR）" >&2
  exit 1
fi

if [[ "$warn" -ne 0 ]]; then
  echo "==> 检查通过，但有 WARN"
  exit 0
fi

echo "==> 检查通过"
