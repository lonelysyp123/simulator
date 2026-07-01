#!/usr/bin/env python3
"""TC-R01: 触发与监控同一点位（默认 bank yx5），记录 T0/T1/ΔT。"""
import re
import subprocess
import time
from datetime import datetime
from typing import Optional

HOST = "127.0.0.1"
BMS_PORT = 1501
SLAVE = 1
ADDR = 1005
LABEL = "yx5"
POLL_S = 0.05
ROUNDS = 3


def ts_ms() -> str:
    return datetime.now().strftime("%H:%M:%S.") + f"{int(datetime.now().microsecond / 1000):03d}"


def now_ms() -> int:
    return int(time.time() * 1000)


def read_coil(port: int, addr: int, slave: int) -> Optional[str]:
    r = subprocess.run(
        ["mbpoll", "-0", "-t", "0", "-a", str(slave), "-r", str(addr), "-c", "1",
         "-p", str(port), "-1", "-q", HOST],
        capture_output=True, text=True,
    )
    m = re.search(rf"\[{addr}\]:\s*(\d+)", r.stdout)
    return m.group(1) if m else None


def write_coil(port: int, addr: int, slave: int, val: int) -> str:
    subprocess.run(
        ["mbpoll", "-0", "-t", "0", "-a", str(slave), "-r", str(addr),
         "-p", str(port), "-1", HOST, str(val)],
        capture_output=True, text=True,
    )
    return ts_ms()


def wait_value(t0_ms: int, port: int, addr: int, slave: int, expect: int, timeout: float = 5.0):
    deadline = t0_ms + int(timeout * 1000)
    while now_ms() < deadline:
        v = read_coil(port, addr, slave)
        if v == str(expect):
            return ts_ms(), now_ms() - t0_ms, v
        time.sleep(POLL_S)
    return None, None, read_coil(port, addr, slave)


def run_round(i: int):
    print(f"\n--- 第 {i} 轮 ---")
    write_coil(BMS_PORT, ADDR, SLAVE, 0)
    time.sleep(0.6)
    baseline = read_coil(BMS_PORT, ADDR, SLAVE)
    print(f"  基线 {LABEL}(slave{SLAVE}@{ADDR})={baseline}")

    t0_str = write_coil(BMS_PORT, ADDR, SLAVE, 1)
    t0_ms = now_ms()
    print(f"  T0 触发写入 @ {t0_str}  ({LABEL} addr {ADDR}=1)")

    t1, dt, val = wait_value(t0_ms, BMS_PORT, ADDR, SLAVE, 1)
    print(f"  T1={t1} ΔT={dt}ms val={val}")

    t0_reset = write_coil(BMS_PORT, ADDR, SLAVE, 0)
    t0_reset_ms = now_ms()
    _, reset_dt, _ = wait_value(t0_reset_ms, BMS_PORT, ADDR, SLAVE, 0)
    print(f"  复位 @ {t0_reset}  →0 ΔT={reset_dt}ms")

    ok = val == "1" and dt is not None and dt <= 700
    return {"t0": t0_str, "t1": t1, "dt": dt, "val": val, "reset_dt": reset_dt, "ok": ok}


def main():
    print(f"TC-R01 自动化 ({LABEL} 写入 → 同点监控)")
    print(f"时间 {datetime.now().isoformat(timespec='seconds')}")

    v = read_coil(BMS_PORT, ADDR, SLAVE)
    print(f"  预检 slave{SLAVE} addr{ADDR} = {v}")
    if v is None:
        print("错误: mbpoll 无法读取 BMS，请确认仿真器已启动")
        return 1

    results = [run_round(i) for i in range(1, ROUNDS + 1)]
    dts = [r["dt"] for r in results if r["dt"] is not None]
    print("\n=== 汇总 ===")
    if dts:
        print(f"  ΔT: avg={sum(dts) // len(dts)}ms max={max(dts)}ms min={min(dts)}ms")
        print(f"  结论: {'通过' if all(r['ok'] for r in results) else '部分通过/失败'} (标准 ≤700ms)")
    else:
        print("  未观察到变位")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
