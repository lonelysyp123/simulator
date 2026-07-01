#!/usr/bin/env python3
"""TC-R05/R06: mbpoll 写 PCS1 启停线圈 yx3，监控仿真器运行状态 yc44，记录 ΔT。"""
import re
import subprocess
import time
from datetime import datetime
from typing import Callable, Optional

HOST = "127.0.0.1"
EMU_PORT = 1601
SLAVE = 1
COIL_ADDR = 1003
STATUS_ADDR = 10053
YT0_ADDR = 40000
POLL_S = 0.05
ROUNDS = 10
TIMEOUT_S = 1.5
POWER_RAW = 500


def ts_ms() -> str:
    return datetime.now().strftime("%H:%M:%S.") + f"{int(datetime.now().microsecond / 1000):03d}"


def now_ms() -> int:
    return int(time.time() * 1000)


def read_coil() -> Optional[int]:
    r = subprocess.run(
        ["mbpoll", "-0", "-t", "0", "-a", str(SLAVE), "-r", str(COIL_ADDR), "-c", "1",
         "-p", str(EMU_PORT), "-1", "-q", HOST],
        capture_output=True, text=True,
    )
    m = re.search(rf"\[{COIL_ADDR}\]:\s*(\d+)", r.stdout)
    return int(m.group(1)) if m else None


def read_status() -> Optional[int]:
    r = subprocess.run(
        ["mbpoll", "-0", "-t", "3", "-a", str(SLAVE), "-r", str(STATUS_ADDR), "-c", "1",
         "-p", str(EMU_PORT), "-1", "-q", HOST],
        capture_output=True, text=True,
    )
    m = re.search(rf"\[{STATUS_ADDR}\]:\s*(-?\d+)", r.stdout)
    return int(m.group(1)) if m else None


def write_coil(val: int) -> str:
    subprocess.run(
        ["mbpoll", "-0", "-t", "0", "-a", str(SLAVE), "-r", str(COIL_ADDR),
         "-p", str(EMU_PORT), "-1", HOST, str(val)],
        capture_output=True, text=True,
    )
    return ts_ms()


def write_yt0(raw: int):
    subprocess.run(
        ["mbpoll", "-0", "-t", "4", "-a", str(SLAVE), "-r", str(YT0_ADDR),
         "-p", str(EMU_PORT), "-1", HOST, str(raw)],
        capture_output=True, text=True,
    )


def wait_coil(t0_ms: int, expect: int):
    deadline = t0_ms + int(TIMEOUT_S * 1000)
    while now_ms() < deadline:
        v = read_coil()
        if v == expect:
            return ts_ms(), now_ms() - t0_ms, v
        time.sleep(POLL_S)
    return None, None, read_coil()


def wait_status(t0_ms: int, ok: Callable[[int], bool]):
    deadline = t0_ms + int(TIMEOUT_S * 1000)
    while now_ms() < deadline:
        s = read_status()
        if s is not None and ok(s):
            return ts_ms(), now_ms() - t0_ms, s
        time.sleep(POLL_S)
    return None, None, read_status()


def prep_stopped():
    write_yt0(POWER_RAW)
    write_coil(0)
    time.sleep(0.8)
    wait_status(now_ms(), lambda s: s == 0)


def run_start(i: int):
    print(f"\n--- 启动 第 {i} 轮 ---")
    prep_stopped()
    print(f"  基线 yx3={read_coil()} yc44={read_status()} (0停机 1待机 2故障 3充电 4放电)")

    t0_str = write_coil(1)
    t0_ms = now_ms()
    print(f"  T0 写 yx3=1 @ {t0_str}")

    coil_t1, coil_dt, coil_v = wait_coil(t0_ms, 1)
    stat_t1, stat_dt, stat_v = wait_status(t0_ms, lambda s: s in (1, 3, 4))

    print(f"  线圈 yx3: T1={coil_t1} ΔT={coil_dt}ms val={coil_v}")
    print(f"  状态 yc44: T1={stat_t1} ΔT={stat_dt}ms val={stat_v}")

    ok = stat_dt is not None and stat_dt <= 700
    return {
        "t0": t0_str, "coil_t1": coil_t1, "coil_dt": coil_dt,
        "stat_t1": stat_t1, "stat_dt": stat_dt, "stat_v": stat_v, "ok": ok,
    }


def run_stop(i: int):
    print(f"\n--- 停机 第 {i} 轮 ---")
    write_coil(1)
    time.sleep(0.5)
    wait_status(now_ms(), lambda s: s in (1, 3, 4))

    t0_str = write_coil(0)
    t0_ms = now_ms()
    print(f"  T0 写 yx3=0 @ {t0_str}  (基线 yc44={read_status()})")

    coil_t1, coil_dt, coil_v = wait_coil(t0_ms, 0)
    stat_t1, stat_dt, stat_v = wait_status(t0_ms, lambda s: s == 0)

    print(f"  线圈 yx3: T1={coil_t1} ΔT={coil_dt}ms val={coil_v}")
    print(f"  状态 yc44: T1={stat_t1} ΔT={stat_dt}ms val={stat_v}")

    ok = stat_dt is not None and stat_dt <= 700
    return {
        "t0": t0_str, "coil_t1": coil_t1, "coil_dt": coil_dt,
        "stat_t1": stat_t1, "stat_dt": stat_dt, "stat_v": stat_v, "ok": ok,
    }


def summarize(label: str, results: list):
    dts = [r["stat_dt"] for r in results if r["stat_dt"] is not None]
    print(f"\n=== {label} yc44 响应 ===")
    if dts:
        print(f"  ΔT: avg={sum(dts)//len(dts)}ms max={max(dts)}ms min={min(dts)}ms")
        passed = sum(1 for r in results if r["ok"])
        print(f"  达标(≤700ms): {passed}/{len(results)}")
    else:
        print("  未观察到 yc44 变位")


def main():
    print("TC-R05/R06: mbpoll 控制 PCS1 启停 (yx3) → 监控运行状态 (yc44)")
    print(f"时间 {datetime.now().isoformat(timespec='seconds')}")

    if read_coil() is None or read_status() is None:
        print("错误: 无法读取 EMU，请确认仿真器端口 1601 可用")
        return 1

    starts = [run_start(i) for i in range(1, ROUNDS + 1)]
    stops = [run_stop(i) for i in range(1, ROUNDS + 1)]

    summarize("启动", starts)
    summarize("停机", stops)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
