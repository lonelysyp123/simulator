#!/usr/bin/env python3
"""
私有签发脚本（勿随社区版公开发布）。

用法：
  export ESS_LICENSE_SECRET='与软件内一致的密钥'   # 生产必改
  ./scripts/license/issue-license.py <机器码> [--years 1] [--expires YYYY-MM-DD] [-o license.txt]

示例：
  ./scripts/license/issue-license.py a1b2c3d4e5f6789012345678abcdef01 -o /tmp/license.txt
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import hmac
import json
import os
import sys
from datetime import date, timedelta

PREFIX = "ESSLIC1"
DEFAULT_SECRET = "EssSimulator-License-Dev-ChangeMe-2026"


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).decode("ascii").rstrip("=")


def issue(machine_id: str, expires: date, secret: str, issued: date | None = None) -> str:
    mid = machine_id.strip().lower()
    if len(mid) != 32 or any(c not in "0123456789abcdef" for c in mid):
        raise SystemExit(f"机器码格式错误（需要 32 位十六进制）: {machine_id!r}")
    payload = {
        "MachineId": mid,
        "Expires": expires.isoformat(),
        "Issued": (issued or date.today()).isoformat(),
    }
    # 与 System.Text.Json 默认 PascalCase 属性名一致
    body = b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    sig = b64url(hmac.new(secret.encode("utf-8"), body.encode("utf-8"), hashlib.sha256).digest())
    return f"{PREFIX}.{body}.{sig}"


def main() -> None:
    ap = argparse.ArgumentParser(description="为 EssSimulator 签发 license.txt")
    ap.add_argument("machine_id", help="用户提供的 32 位机器码")
    ap.add_argument("--years", type=int, default=1, help="有效年限（默认 1 年）")
    ap.add_argument("--expires", help="到期日 YYYY-MM-DD（优先于 --years）")
    ap.add_argument("-o", "--output", default="license.txt", help="输出文件（默认 license.txt）")
    ap.add_argument("--secret-file", default="", help="密钥文件路径（默认读环境变量 ESS_LICENSE_SECRET）")
    args = ap.parse_args()

    secret = os.environ.get("ESS_LICENSE_SECRET", "").strip()
    if args.secret_file:
        secret = open(args.secret_file, encoding="utf-8").read().strip()
    if not secret:
        secret = DEFAULT_SECRET
        print("警告: 未设置 ESS_LICENSE_SECRET，使用开发默认密钥（勿用于正式交付）", file=sys.stderr)

    if args.expires:
        expires = date.fromisoformat(args.expires)
    else:
        expires = date.today() + timedelta(days=365 * max(1, args.years))

    token = issue(args.machine_id, expires, secret)
    content = (
        "# EssSimulator license — 请放在程序运行目录\n"
        f"# machine={args.machine_id.strip().lower()}\n"
        f"# expires={expires.isoformat()}\n"
        f"{token}\n"
    )
    with open(args.output, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"已写入 {args.output}")
    print(f"到期日: {expires.isoformat()}")
    print(token)


if __name__ == "__main__":
    main()
