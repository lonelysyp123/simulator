#!/usr/bin/env bash
# 轮询 bms1~bms8 的 SOC(yc11)、并网状态(yc0)、三级报警(yc6)，SOC≥90% 或异常时高亮。
set -euo pipefail

HOST="${1:-127.0.0.1}"
BASE_PORT="${2:-1501}"
COUNT="${3:-8}"
INTERVAL="${4:-5}"

read_reg() {
  python3 - "$HOST" "$1" "$2" <<'PY'
import socket, struct, sys
host, port, addr = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
req = struct.pack(">HHHBBHH", 1, 0, 6, 1, 4, addr, 1)
s = socket.create_connection((host, port), timeout=1.0)
s.sendall(req)
resp = s.recv(256)
s.close()
if len(resp) < 11 or resp[7] & 0x80:
    print("ERR")
else:
    print(struct.unpack(">H", resp[9:11])[0])
PY
}

while true; do
  ts=$(date '+%H:%M:%S')
  line="[$ts]"
  alert=""
  for i in $(seq 1 "$COUNT"); do
    port=$((BASE_PORT + i - 1))
    soc_raw=$(read_reg "$port" 10011 || echo ERR)
    yc0=$(read_reg "$port" 10000 || echo ERR)
    yc6=$(read_reg "$port" 10006 || echo ERR)
    if [[ "$soc_raw" == ERR ]]; then
      soc_pct="?"
    else
      soc_pct=$(awk "BEGIN {printf \"%.1f\", $soc_raw/10}")
    fi
    line+=" bms${i}:SOC=${soc_pct}% st=${yc0} flt=${yc6}"
    if [[ "$soc_raw" != ERR && "$soc_raw" -ge 900 ]]; then
      alert+=" [WARN bms${i} SOC高]"
    fi
    if [[ "$yc0" == "3" || "$yc6" != "0" && "$yc6" != ERR ]]; then
      alert+=" [ALERT bms${i} yc0=${yc0} yc6=${yc6}]"
    fi
  done
  if [[ -n "$alert" ]]; then
    echo "${line}${alert}"
  else
    echo "$line"
  fi
  sleep "$INTERVAL"
done
