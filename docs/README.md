# 仿真模拟器（EssSimulator）文档索引

对外产品名：**仿真模拟器**。工程名：EssSimulator。

| 文档 | 读者 | 说明 |
|------|------|------|
| [系统设计说明](./系统设计说明.md) | 开发、架构 | 技术栈、分层架构、数据流、设计原则 |
| [用户手册](./用户手册.md) | EMS/BMS 联调、测试 | 安装启动、GUI、Modbus 端口、典型联调流程 |
| [项目编译说明](./项目编译说明.md) | 构建、发布 | 开发版/商业版脚本、dist 目录、版本差异 |
| [产品分档与交付边界](./产品分档与交付边界.md) | 销售、交付、发布 | 社区版/充值版/定制版定位与权益边界 |
| [充值版授权网关约定](./充值版授权网关约定.md) | 运维、接入方 | 仓外计费/鉴权与仿真链路开关如何协作 |
| [测试报告](./测试报告.md) | 测试、QA | 性能与 DataExchange 手动测试记录 |
| [指令详细说明](./指令详细说明.md) | 联调、测试 | esscmd / dpc / breaker / dpctest 用法与示例 |
| [appsettings 字段说明](./appsettings.explained.md) | 开发、集成 | `appsettings.json` 各配置段含义 |
| [点位表版本说明](../pointmaps/README.md) | 联调、发布 | common / lc / battery 与发布参数 |

## 补充材料

- [博客与设计笔记](./blog/)：需求、困境、总结等过程文档
- 随包 `scripts/README.txt`：社区版压缩包内的简明说明（非 Markdown）

## 文档约定

- 点表、端口、单元数以仓库根目录 **`appsettings.json`** 与 **`*.csv`** 为准，文档中的数字仅为示例。
- Modbus 命令生成规则见仓库技能 `ess-mbpoll`（按 CSV 的 FunctionCode 选 `-t`，禁止默认 `-t 4`）。
