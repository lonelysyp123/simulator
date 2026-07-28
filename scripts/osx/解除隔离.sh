#!/usr/bin/env bash
# 解除 macOS 对从网络下载文件的隔离标记（Gatekeeper 拦截）
set -euo pipefail
cd "$(dirname "$0")"

echo "正在移除 com.apple.quarantine 隔离属性…"
xattr -dr com.apple.quarantine . 2>/dev/null || true

if [[ -f ./EssSimulator ]]; then
  chmod +x ./EssSimulator
fi
if [[ -f ./start.sh ]]; then
  chmod +x ./start.sh
fi
if [[ -f ./解除隔离.sh ]]; then
  chmod +x ./解除隔离.sh
fi

echo "完成。请再执行：./start.sh"
echo
echo "若仍被拦截：系统设置 → 隐私与安全性 → 找到 EssSimulator →「仍要打开」"
echo "或在 Finder 中对 EssSimulator 按住 Control 点击 → 打开 → 打开"
