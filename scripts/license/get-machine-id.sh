#!/usr/bin/env bash
# 获取本机机器码（与 EssSimulator --machine-id / C# MachineIdProvider 算法一致）
set -euo pipefail

raw=""
if [[ "$(uname -s)" == "Darwin" ]]; then
  raw="$(ioreg -rd1 -c IOPlatformExpertDevice 2>/dev/null | awk -F'"' '/IOPlatformUUID/{print $4; exit}')"
elif [[ "$(uname -s)" == "Linux" ]]; then
  if [[ -f /etc/machine-id ]]; then
    raw="$(tr -d ' \n\r\t' </etc/machine-id)"
  elif [[ -f /var/lib/dbus/machine-id ]]; then
    raw="$(tr -d ' \n\r\t' </var/lib/dbus/machine-id)"
  fi
else
  echo "请在 Windows 上使用 get-machine-id.ps1，或运行: EssSimulator --machine-id" >&2
  exit 1
fi

if [[ -z "${raw}" ]]; then
  raw="$(hostname)|$(whoami)|$(uname -s)"
fi

# SHA256("EssSimulator|" + lower(trim(raw))) 前 16 字节 → 32 hex
python3 - "$raw" <<'PY'
import hashlib, sys
raw = sys.argv[1].strip().lower()
digest = hashlib.sha256(("EssSimulator|" + raw).encode("utf-8")).digest()
print(digest[:16].hex())
PY
