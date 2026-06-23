#!/usr/bin/env python3
"""TC-R02: 触发与监控同一点位（EMU yx1 低压断路器），记录 T0/T1/ΔT。"""
import re
import subprocess
import time
from datetime import datetime
from typing import Optional

HOST = "127.0.0.1"
EMU_PORT = 1601
SLAVE = 1
ADDR = 1001
LABEL = "yx1"
POLL_S = 0.05
ROUNDS = 3
TIMEOUT_S = 5.0


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


def wait_value(t0_ms: int, port: int, addr: int, slave: int, expect: int):
    deadline = t0_ms + int(TIMEOUT_S * 1000)
    while now_ms() < deadline:
        v = read_coil(port, addr, slave)
        if v == str(expect):
            return ts_ms(), now_ms() - t0_ms, v
        time.sleep(POLL_S)
    return None, None, read_coil(port, addr, slave)


def prep_value(port: int, addr: int, slave: int, val: int):
    write_coil(port, addr, slave, val)
    time.sleep(0.4)
    t0 = now_ms()
    _, _, v = wait_value(t0, port, addr, slave, val)
    return v


def run_round(i: int, op: str, expect: int, prep: int):
    print(f"\n--- {op} 第 {i} 轮 ---")
    baseline = prep_value(EMU_PORT, ADDR, SLAVE, prep)
    print(f"  基线 {LABEL}(slave{SLAVE}@{ADDR})={baseline}")

    t0_str = write_coil(EMU_PORT, ADDR, SLAVE, expect)
    t0_ms = now_ms()
    print(f"  T0 触发写入 @ {t0_str}  ({LABEL} addr {ADDR}={expect})")

    t1, dt, val = wait_value(t0_ms, EMU_PORT, ADDR, SLAVE, expect)
    print(f"  T1={t1} ΔT={dt}ms val={val}")

    ok = val == str(expect) and dt is not None and dt <= 700
    return {"t0": t0_str, "t1": t1, "dt": dt, "val": val, "ok": ok}


def summarize(label: str, results: list):
    dts = [r["dt"] for r in results if r["dt"] is not None]
    print(f"\n=== {label} 汇总 ===")
    if dts:
        avg = sum(dts) // len(dts)
        print(f"  ΔT: avg={avg}ms max={max(dts)}ms min={min(dts)}ms")
        print(f"  结论: {'通过' if all(r['ok'] for r in results) else '部分通过/失败'} (标准 ≤700ms)")
        return avg, max(dts), min(dts), all(r["ok"] for r in results)
    print("  未观察到变位")
    return None, None, None, False


def main():
    print(f"TC-R02 自动化 ({LABEL} 写入 → 同点监控)")
    print(f"时间 {datetime.now().isoformat(timespec='seconds')}")

    v = read_coil(EMU_PORT, ADDR, SLAVE)
    print(f"  预检 slave{SLAVE} addr{ADDR} = {v}")
    if v is None:
        print("错误: mbpoll 无法读取 EMU，请确认仿真器已启动且端口 1601 可用")
        return 1

    close_results = [run_round(i, "合闸 set 1", 1, 0) for i in range(1, ROUNDS + 1)]
    open_results = [run_round(i, "分闸 set 0", 0, 1) for i in range(1, ROUNDS + 1)]

    summarize("合闸 set 1", close_results)
    summarize("分闸 set 0", open_results)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
