#!/usr/bin/env python3
"""TC-R03: BMS rack yt4 触发与监控同一点（FC6 保持寄存器），记录 T0/T1/ΔT。"""
import re
import subprocess
import time
from datetime import datetime
from typing import Optional

HOST = "127.0.0.1"
BMS_PORT = 1501
SLAVE = 2
ADDR = 40004
LABEL = "yt4"
SCALE = 10
POLL_S = 0.05
ROUNDS = 3
TIMEOUT_S = 5.0
# 物理量 1350.0 / 1360.0 / 1370.0 V → raw = 物理量 × Scale
TEST_RAWS = [13500, 13600, 13700]
RESET_RAW = 0


def ts_ms() -> str:
    return datetime.now().strftime("%H:%M:%S.") + f"{int(datetime.now().microsecond / 1000):03d}"


def now_ms() -> int:
    return int(time.time() * 1000)


def read_reg(port: int, addr: int, slave: int) -> Optional[int]:
    r = subprocess.run(
        ["mbpoll", "-0", "-t", "4", "-a", str(slave), "-r", str(addr), "-c", "1",
         "-p", str(port), "-1", "-q", HOST],
        capture_output=True, text=True,
    )
    m = re.search(rf"\[{addr}\]:\s*(-?\d+)", r.stdout)
    return int(m.group(1)) if m else None


def write_reg(port: int, addr: int, slave: int, raw: int) -> str:
    subprocess.run(
        ["mbpoll", "-0", "-t", "4", "-a", str(slave), "-r", str(addr),
         "-p", str(port), "-1", HOST, str(raw)],
        capture_output=True, text=True,
    )
    return ts_ms()


def wait_reg(t0_ms: int, port: int, addr: int, slave: int, expect: int):
    deadline = t0_ms + int(TIMEOUT_S * 1000)
    while now_ms() < deadline:
        v = read_reg(port, addr, slave)
        if v == expect:
            return ts_ms(), now_ms() - t0_ms, v
        time.sleep(POLL_S)
    return None, None, read_reg(port, addr, slave)


def run_round(i: int, raw: int):
    print(f"\n--- 第 {i} 轮 (raw={raw}, 物理≈{raw / SCALE:.1f}) ---")
    write_reg(BMS_PORT, ADDR, SLAVE, RESET_RAW)
    time.sleep(0.5)
    baseline = read_reg(BMS_PORT, ADDR, SLAVE)
    print(f"  基线 {LABEL}(slave{SLAVE}@{ADDR})={baseline}")

    t0_str = write_reg(BMS_PORT, ADDR, SLAVE, raw)
    t0_ms = now_ms()
    print(f"  T0 触发写入 @ {t0_str}  ({LABEL}={raw})")

    t1, dt, val = wait_reg(t0_ms, BMS_PORT, ADDR, SLAVE, raw)
    print(f"  T1={t1} ΔT={dt}ms val={val}")

    ok = val == raw and dt is not None and dt <= 700
    return {"t0": t0_str, "t1": t1, "dt": dt, "val": val, "raw": raw, "ok": ok}


def main():
    print(f"TC-R03 自动化 (BMS rack {LABEL} 写入 → 同点监控, FC6/-t 4)")
    print(f"时间 {datetime.now().isoformat(timespec='seconds')}")

    v = read_reg(BMS_PORT, ADDR, SLAVE)
    print(f"  预检 slave{SLAVE} addr{ADDR} = {v}")
    if v is None:
        print("错误: mbpoll 无法读取 BMS rack，请确认仿真器已启动")
        return 1

    results = [run_round(i, TEST_RAWS[i - 1]) for i in range(1, ROUNDS + 1)]
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
