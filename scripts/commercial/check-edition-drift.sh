#!/usr/bin/env bash
# 商业档位配置差异与定制版漂移检查（发布前建议执行）
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMMUNITY="$ROOT/configs/社区版.appsettings.json"
RECHARGE="$ROOT/configs/充值版.appsettings.json"
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

echo "==> 检查商业档位配置差异"
if [[ ! -f "$COMMUNITY" || ! -f "$RECHARGE" || ! -f "$CUSTOM" ]]; then
  echo "ERROR: configs/ 下缺少社区版/充值版/定制版模板" >&2
  exit 1
fi

c_bind="$(json_get "$COMMUNITY" "Simulator.Protocol.BindAddress")"
r_bind="$(json_get "$RECHARGE" "Simulator.Protocol.BindAddress")"
c_http="$(json_get "$COMMUNITY" "Simulator.Observability.HttpBindAddress")"
r_http="$(json_get "$RECHARGE" "Simulator.Observability.HttpBindAddress")"
c_gui="$(json_get "$COMMUNITY" "Simulator.Runtime.NoGui")"
r_gui="$(json_get "$RECHARGE" "Simulator.Runtime.NoGui")"
c_units="$(json_get "$COMMUNITY" "EssUnits")"
r_units="$(json_get "$RECHARGE" "EssUnits")"
custom_units="$(json_get "$CUSTOM" "EssUnits")"
root_units="$(json_get "$ROOT_CFG" "EssUnits")"

echo "    社区版 BindAddress=$c_bind HttpBind=$c_http NoGui=$c_gui Units=$c_units"
echo "    充值版 BindAddress=$r_bind HttpBind=$r_http NoGui=$r_gui Units=$r_units"
echo "    定制版 Units=$custom_units  |  根 appsettings Units=$root_units"

if [[ "$c_bind" != "127.0.0.1" ]]; then
  echo "ERROR: 社区版 Protocol.BindAddress 应为 127.0.0.1，当前=$c_bind" >&2
  fail=1
fi
if [[ "$c_http" != "127.0.0.1" ]]; then
  echo "ERROR: 社区版 Observability.HttpBindAddress 应为 127.0.0.1，当前=$c_http" >&2
  fail=1
fi
if [[ "$c_gui" != "false" ]]; then
  echo "ERROR: 社区版 Runtime.NoGui 应为 false，当前=$c_gui" >&2
  fail=1
fi

if [[ "$r_bind" == "127.0.0.1" || -z "$r_bind" ]]; then
  echo "ERROR: 充值版 Protocol.BindAddress 不应为本机回环（期望 0.0.0.0 等可托管地址），当前=$r_bind" >&2
  fail=1
fi
if [[ "$r_http" == "127.0.0.1" || -z "$r_http" ]]; then
  echo "ERROR: 充值版 HttpBindAddress 不应为本机回环，当前=$r_http" >&2
  fail=1
fi
if [[ "$r_gui" != "true" ]]; then
  echo "ERROR: 充值版 Runtime.NoGui 应为 true（无头托管），当前=$r_gui" >&2
  fail=1
fi

if [[ "$c_units" != "$r_units" ]]; then
  echo "ERROR: 社区版与充值版 EssUnits 单元数应一致（$c_units vs $r_units）" >&2
  fail=1
fi

if [[ "$custom_units" != "$root_units" ]]; then
  echo "WARN: 定制版与根 appsettings.json 的 EssUnits 单元数不一致（$custom_units vs $root_units）" >&2
  echo "      若定制版是项目快照可忽略；若应对齐开发主配置，请同步后再发布。" >&2
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
