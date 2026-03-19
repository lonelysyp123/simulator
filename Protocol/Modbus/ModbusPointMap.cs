using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>
    /// 负责从 CSV 文件读取 Modbus 点位表，并建立以下索引：
    ///   - dataMaps / controlMaps：按功能码分类的主设备点表
    ///   - rackDataMaps / rackControlMaps：BMS rack 级点表
    ///   - paramModelLookup / rackParamModelLookup：点名 → 模拟模型
    ///   - modelParamLookup / rackModelParamLookup：模型类型 → 点列表（用于分组 Worker）
    ///   - defaultBuffer：CSV 中直接给定固定值的点
    /// </summary>
    public class ModbusPointMap
    {
        public List<MapEntry> DataMaps    { get; } = new();
        public List<MapEntry> ControlMaps { get; } = new();
        public List<MapEntry> RackDataMaps    { get; } = new();
        public List<MapEntry> RackControlMaps { get; } = new();

        public Dictionary<string, ModesimModel>    ParamModelLookup     { get; } = new();
        public Dictionary<string, ModesimModel>    RackParamModelLookup { get; } = new();
        public Dictionary<string, List<MapEntry>>  ModelParamLookup     { get; } = new();
        public Dictionary<string, List<MapEntry>>  RackModelParamLookup { get; } = new();
        public Dictionary<string, object>          DefaultBuffer        { get; } = new();

        /// <summary>所有点位原始数组，按 [bank, rack, ...] 顺序，供 ModbusTCPSlave 使用</summary>
        public List<MapEntry[]> RawMaps { get; } = new();

        public ModbusPointMap(string mapFilePath, string serverName, int clusterCount = 0)
        {
            LoadBankMap(mapFilePath, serverName);

            if (serverName.ToLower().Contains("bms") && clusterCount > 0)
                LoadRackMap(mapFilePath, serverName);
        }

        // ── 私有加载方法 ──────────────────────────────────────────────

        private void LoadBankMap(string mapFilePath, string name)
        {
            var entries = CSVUtil.CSV2Class<MapEntry>(mapFilePath)?.ToArray()
                ?? throw new Exception($"Modbus bank map 读取失败: {mapFilePath}");

            ApplyDeviceIdSubstitution(entries, name, isEmu: name.Contains("Emu"));
            RawMaps.Add(entries);

            foreach (var entry in entries)
            {
                var model = ModbusSimServer.GetModelParam(entry.ModelSim!);
                if (model == null)
                {
                    if (!string.IsNullOrWhiteSpace(entry.ModelSim) &&
                        float.TryParse(entry.ModelSim, out var dv))
                        DefaultBuffer[entry.ParamName!] = dv;
                    continue;
                }

                ParamModelLookup[entry.ParamName!] = model;
                if (string.IsNullOrWhiteSpace(model.ModelType) || entry.FunctionCode == 6) continue;

                if (!ModelParamLookup.TryGetValue(model.ModelType, out var list))
                    ModelParamLookup[model.ModelType] = list = new List<MapEntry>();
                list.Add(entry);
            }

            DataMaps.AddRange(entries.Where(m => m.FunctionCode is 3 or 4));
            ControlMaps.AddRange(entries.Where(m => m.FunctionCode == 6));
        }

        private void LoadRackMap(string mapFilePath, string serverName)
        {
            string rackPath = mapFilePath.Replace("bank", "rack");
            var entries = CSVUtil.CSV2Class<MapEntry>(rackPath)?.ToArray()
                ?? throw new Exception($"Modbus rack map 读取失败: {rackPath}");

            ApplyDeviceIdSubstitution(entries, serverName, isEmu: false);
            RawMaps.Add(entries);

            foreach (var entry in entries)
            {
                var model = ModbusSimServer.GetModelParam(entry.ModelSim!);
                if (model == null) continue;

                RackParamModelLookup[entry.ParamName!] = model;
                if (string.IsNullOrWhiteSpace(model.ModelType) || entry.FunctionCode == 6) continue;

                if (!RackModelParamLookup.TryGetValue(model.ModelType, out var list))
                    RackModelParamLookup[model.ModelType] = list = new List<MapEntry>();
                list.Add(entry);
            }

            RackDataMaps.AddRange(entries.Where(m => m.FunctionCode is 3 or 4));
            RackControlMaps.AddRange(entries.Where(m => m.FunctionCode == 6));
        }

        private static void ApplyDeviceIdSubstitution(MapEntry[] entries, string name, bool isEmu)
        {
            if (!int.TryParse(new string(name.Where(char.IsDigit).ToArray()), out int deviceId))
                return;

            if (isEmu) return; // EMU 不替换 deviceId

            foreach (var e in entries)
            {
                if (e.ModelSim != null)
                    e.ModelSim = e.ModelSim.Replace("deviceId", deviceId.ToString());
            }
        }
    }
}
