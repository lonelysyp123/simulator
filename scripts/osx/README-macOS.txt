EssSimulator - macOS 快速说明
================================

完整使用说明请阅读同目录下的 README.txt。

快速启动
--------
1. 解压 EssSimulator-*-osx-*.tar.gz
2. chmod +x EssSimulator start.sh && ./start.sh
3. 浏览器访问 http://localhost:5050（脚本会尝试自动打开）

若系统提示「无法打开，因为无法验证开发者」：
  系统设置 → 隐私与安全性 → 仍要打开
  或：xattr -dr com.apple.quarantine .

默认 Modbus 端口：电表 1500，BMS1 1501，EMU1 1601（详见 README.txt）

详细说明见 README.txt；技术手册见 docs/用户手册.md（若已随包提供）
