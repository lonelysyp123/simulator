# scripts/ 目录说明

本目录包含仿真模拟器的构建脚本、平台启动辅助文件及测试校验脚本。

## 非商业发布（开发 / 联调）

直接使用仓库根配置 `appsettings.json`，输出到 `dist/win-x64` 或 `dist/linux-arm64`：

| 脚本 | 说明 |
|------|------|
| [`publish-windows.sh`](publish-windows.sh) | Windows x64 自包含单文件 + zip |
| [`publish-linux.sh`](publish-linux.sh) | Linux 自包含单文件 + tar.gz（`RID`、`CONFIG` 可环境变量覆盖） |

```bash
./scripts/publish-windows.sh
./scripts/publish-linux.sh
RID=linux-x64 CONFIG=Release ./scripts/publish-linux.sh
```

## 商业发布（多版本产品包）

社区版、充值版、定制版的分版本打包见 **[`commercial/README.md`](commercial/README.md)**。

```bash
./scripts/commercial/publish-all.sh
EDITION=定制版 ./scripts/commercial/publish-windows.sh
```

## 平台辅助

| 目录 | 内容 |
|------|------|
| [`windows/`](windows/) | `start.bat`、`README-Windows.txt` |
| [`linux/`](linux/) | `start.sh`、`README-Linux.txt` |

## 测试与校验

脚本位于 [`test/`](test/) 目录：

| 脚本 / 目录 | 说明 |
|-------------|------|
| [`test/perf/`](test/perf/) | 性能/回归 Python 脚本 |
| [`test/validate-*-dataexchange.sh`](test/) | 点表与数据交换校验（需仿真器已运行） |
| [`test/run-*-dataexchange-validation.sh`](test/) | 一键启动仿真器并执行校验 |

```bash
./scripts/test/run-emu-dataexchange-validation.sh
./scripts/test/run-bms-dataexchange-validation.sh
python3 scripts/test/perf/run_tc_r01.py
```

## 随包文档

[`README.txt`](README.txt) 为社区版运行说明模板；商业各版本说明在 [`commercial/editions/`](commercial/editions/)。
