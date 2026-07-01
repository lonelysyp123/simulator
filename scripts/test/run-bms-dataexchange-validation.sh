#!/usr/bin/env bash
# 单单元 BMS/EMU DataExchange 本地验证
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

BACKUP="$ROOT/appsettings.json.bak.bms-validation"
LOG=/tmp/ess-bms-dx-validation.log
BIN_LOG_DIR="$ROOT/bin/Release/net8.0/Logs"
SIM_PID=""

free_port() {
  local port=$1
  local pids
  pids=$(lsof -t -i :"$port" -sTCP:LISTEN 2>/dev/null || true)
  if [[ -n "$pids" ]]; then
    echo "清理占用端口 $port 的进程: $pids"
    kill -9 $pids 2>/dev/null || true
    sleep 1
  fi
}

cleanup() {
  if [[ -n "$SIM_PID" ]] && kill -0 "$SIM_PID" 2>/dev/null; then
    kill "$SIM_PID" 2>/dev/null || true
    wait "$SIM_PID" 2>/dev/null || true
  fi
  if [[ -f "$BACKUP" ]]; then
    mv -f "$BACKUP" "$ROOT/appsettings.json"
    cp -f "$ROOT/appsettings.json" "$ROOT/bin/Release/net8.0/appsettings.json" 2>/dev/null || true
  fi
}
trap cleanup EXIT

echo "=== 准备 BMS 验证配置 ==="
cp -f "$ROOT/appsettings.json" "$BACKUP"
cp -f "$ROOT/appsettings.validation.json" "$ROOT/appsettings.json"

dotnet build -c Release -v q
cp -f "$ROOT/appsettings.json" "$ROOT/bin/Release/net8.0/appsettings.json"

rm -f "$LOG" Logs/*.log "$BIN_LOG_DIR"/*.log 2>/dev/null || true
free_port 1501
free_port 1601

echo "=== 启动仿真器（NoGui）==="
dotnet run -c Release --no-build --no-launch-profile > "$LOG" 2>&1 &
SIM_PID=$!

echo "等待 simBms1 :1501 ..."
for i in $(seq 1 60); do
  if lsof -i :1501 -sTCP:LISTEN >/dev/null 2>&1; then
    echo "端口 1501 就绪 (${i}s)"
    break
  fi
  if ! kill -0 "$SIM_PID" 2>/dev/null; then
    echo "仿真器异常退出"; tail -40 "$LOG"; exit 1
  fi
  sleep 1
done

lsof -i :1501 -sTCP:LISTEN >/dev/null || { echo "超时"; tail -40 "$LOG"; exit 1; }

for i in $(seq 1 30); do
  if grep -q "\[DataExchange\] simBms1 已启动" "$BIN_LOG_DIR"/*.log 2>/dev/null; then
    break
  fi
  sleep 0.5
done
sleep 2

echo "=== 执行 BMS mbpoll 验证 ==="
"$ROOT/scripts/test/validate-bms-dataexchange.sh" "$LOG"
