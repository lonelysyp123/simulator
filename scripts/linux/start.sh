#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo "Starting EssSimulator (B/S architecture)..."
echo "Web UI: http://localhost:5050"
echo "Modbus TCP ports: see appsettings.json -> Simulator.Protocol"
echo

# 尝试在常见桌面环境下打开默认浏览器（失败不影响服务启动）
if command -v xdg-open >/dev/null 2>&1; then
  (sleep 1 && xdg-open http://localhost:5050 >/dev/null 2>&1) &
elif command -v open >/dev/null 2>&1; then
  (sleep 1 && open http://localhost:5050 >/dev/null 2>&1) &
fi

exec ./EssSimulator "$@"
