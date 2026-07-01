#!/usr/bin/env bash
# EMU DataExchange 本地验证（需仿真器已启动且 simEmu1 端口 1601 可连）
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

HOST=127.0.0.1
PORT=1601
SLAVE=1
LOG=${1:-/tmp/ess-emu-dx-validation.log}
BIN_LOG_DIR="$ROOT/bin/Release/net8.0/Logs"

log_has() {
  local pattern=$1
  grep -q "$pattern" "$LOG" 2>/dev/null && return 0
  grep -q "$pattern" Logs/*.log 2>/dev/null && return 0
  grep -q "$pattern" "$BIN_LOG_DIR"/*.log 2>/dev/null && return 0
  return 1
}

mbpoll_run() {
  local tmp rc pid i
  tmp=$(mktemp)
  mbpoll "$@" >"$tmp" 2>&1 &
  pid=$!
  i=0
  while kill -0 "$pid" 2>/dev/null && [[ $i -lt 80 ]]; do
    sleep 0.1
    i=$((i + 1))
  done
  if kill -0 "$pid" 2>/dev/null; then
    kill "$pid" 2>/dev/null || true
    cat "$tmp"
    rm -f "$tmp"
    return 124
  fi
  wait "$pid" || rc=$?
  rc=${rc:-0}
  cat "$tmp"
  rm -f "$tmp"
  return "$rc"
}

pass=0
fail=0

ok() { echo "  [PASS] $1"; pass=$((pass + 1)); }
ng() { echo "  [FAIL] $1"; fail=$((fail + 1)); }

echo "=== EMU DataExchange 本地验证 ==="
echo "日志: $LOG"
echo

if log_has "\[DataExchange\] simEmu1 已启动"; then
  ok "日志含 DataExchange simEmu1 启动"
else
  ng "日志未找到 [DataExchange] simEmu1 已启动（检查 $LOG 或 $BIN_LOG_DIR）"
fi

if lsof -i :"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then
  ok "simEmu1 端口 $PORT 监听中"
else
  ng "端口 $PORT 未监听"
  echo "合计: $pass 通过, $fail 失败"
  exit 1
fi

echo
echo "--- mbpoll 读写 ---"

# FC4 读 PCS1 有功 param8 @25308 (int32, Scale 10)
if out=$(mbpoll_run -0 -1 -t 3:int -a "$SLAVE" -r 25308 -c 2 -p "$PORT" "$HOST"); then
  ok "读 param8(25308) 成功: $(echo "$out" | tail -1)"
else
  ng "读 param8 失败: $out"
fi

# FC0 读 pcs1_startstop @5303（AutoStart 边沿触发后线圈清 0，便于再次写 1）
if out=$(mbpoll_run -0 -1 -t 0 -a "$SLAVE" -r 5303 -p "$PORT" "$HOST"); then
  val=$(echo "$out" | grep -E '\[5303\]' | awk '{print $NF}' | tr -d '[]' || true)
  if [[ "$val" == "0" ]]; then
    ok "读 pcs1_startstop=0 (AutoStart 边沿已处理)"
  else
    ng "读 pcs1_startstop 期望 0（边沿清回），实际 $val"
  fi
else
  ng "读 pcs1_startstop 失败: $out"
fi

# FC4 写 PCS1 有功设定 100kW → param55 @5300 raw=1000 (Scale 10)
if mbpoll_run -0 -t 4 -a "$SLAVE" -r 5300 -p "$PORT" -1 "$HOST" 1000 >/dev/null; then
  ok "写 param55 有功设定 100kW (5300=1000)"
else
  ng "写 param55 失败"
fi

sleep 0.3

# FC0 写 pcs1 停机再启动（验证控制管道 + Effect）
if mbpoll_run -0 -t 0 -a "$SLAVE" -r 5303 -p "$PORT" -1 "$HOST" 0 >/dev/null; then
  ok "写 pcs1_startstop=0"
else
  ng "写 pcs1_startstop=0 失败"
fi

sleep 0.2

if mbpoll_run -0 -t 0 -a "$SLAVE" -r 5303 -p "$PORT" -1 "$HOST" 1 >/dev/null; then
  ok "写 pcs1_startstop=1"
else
  ng "写 pcs1_startstop=1 失败"
fi

sleep 0.5

if log_has "\[EMU-Control:change\].*pcs1_startstop"; then
  ok "日志含 EMU 控制变更 pcs1_startstop"
elif log_has "\[EMU-Feedback:change\].*pcs1_startstop"; then
  ok "日志含 EMU 反馈变更 pcs1_startstop"
else
  ng "日志未找到 EMU-Control/Feedback change pcs1_startstop"
fi

echo
echo "合计: $pass 通过, $fail 失败"
[[ "$fail" -eq 0 ]]
