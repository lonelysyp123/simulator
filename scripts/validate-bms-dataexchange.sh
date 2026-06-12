#!/usr/bin/env bash
# BMS DataExchange 本地验证（需仿真器已启动且 simBms1 端口 1501 可连）
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

HOST=127.0.0.1
PORT=1501
SLAVE=1
LOG=${1:-/tmp/ess-bms-dx-validation.log}
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

echo "=== BMS DataExchange 本地验证 ==="
echo "日志: $LOG"
echo

if log_has "\[DataExchange\] simBms1 已启动"; then
  ok "日志含 DataExchange simBms1 启动"
else
  ng "日志未找到 [DataExchange] simBms1 已启动"
fi

if lsof -i :"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then
  ok "simBms1 端口 $PORT 监听中"
else
  ng "端口 $PORT 未监听"
  echo "合计: $pass 通过, $fail 失败"
  exit 1
fi

echo
echo "--- mbpoll 读写 param11（一键并网 Pulse）---"

# FC6 写 param11=1 @12289
if mbpoll_run -0 -1 -t 4 -a "$SLAVE" -r 12289 -p "$PORT" -1 "$HOST" 1 >/dev/null; then
  ok "写 param11=1 (12289)"
else
  ng "写 param11=1 失败"
fi

sleep 0.8

# Pulse：BmsLinkService 消费后反馈管道清 0
if out=$(mbpoll_run -0 -1 -t 4 -a "$SLAVE" -r 12289 -p "$PORT" "$HOST"); then
  val=$(echo "$out" | grep -E '\[12289\]' | awk '{print $NF}' | tr -d '[]' || true)
  if [[ "$val" == "0" ]]; then
    ok "读 param11=0 (Pulse 已归零)"
  else
    ng "读 param11 期望 0（Pulse），实际 $val"
  fi
else
  ng "读 param11 失败: $out"
fi

if log_has "\[BMS-Control:change\].*param11" || log_has "\[BMS-Feedback:change\].*param11"; then
  ok "日志含 BMS 控制/反馈 param11"
else
  ng "日志未找到 BMS-Control/Feedback param11"
fi

echo
echo "合计: $pass 通过, $fail 失败"
[[ "$fail" -eq 0 ]]
