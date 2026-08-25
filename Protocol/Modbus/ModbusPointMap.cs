using System;
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

        public ModbusPointMap(
            string mapFilePath,
            string serverName,
            int clusterCount = 0,
            int? emuDeviceIdOverride = null)
        {
            var resolvedPath = PointMapPathResolver.Resolve(mapFilePath);
            var entries = CSVUtil.CSV2Class<MapEntry>(resolvedPath)?.ToArray()
                ?? throw new Exception($"Modbus bank map 读取失败: {resolvedPath}");

            ApplyDeviceIdSubstitution(
                entries,
                serverName,
                isEmu: serverName.Contains("Emu", StringComparison.OrdinalIgnoreCase),
                emuDeviceIdOverride);

            IndexBankEntries(entries);
            RawMaps.Add(entries);

            if (resolvedPath.Contains("bms_bank", StringComparison.OrdinalIgnoreCase))
                DefaultBuffer.TryAdd("param4", (ushort)2);

            if (serverName.Contains("bms", StringComparison.OrdinalIgnoreCase) && clusterCount > 0)
                LoadRackMap(PointMapPathResolver.ResolveSibling(resolvedPath, "bms_rack.csv"), serverName);
        }

        private void IndexBankEntries(MapEntry[] entries)
        {
            DataMaps.AddRange(entries.Where(m => m.FunctionCode is 3 or 4));
            ControlMaps.AddRange(entries.Where(m => m.FunctionCode is 5 or 6 or 16));

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

                if (string.IsNullOrWhiteSpace(model.ModelType) || entry.FunctionCode is 5 or 6 or 16)
                    continue;

                if (!ModelParamLookup.TryGetValue(model.ModelType, out var list))
                    ModelParamLookup[model.ModelType] = list = new List<MapEntry>();
                list.Add(entry);
            }
        }

        private void LoadRackMap(string rackPath, string serverName)
        {
            var entries = CSVUtil.CSV2Class<MapEntry>(rackPath)?.ToArray()
                ?? throw new Exception($"Modbus rack map 读取失败: {rackPath}");

            ApplyDeviceIdSubstitution(entries, serverName, isEmu: false);
            RawMaps.Add(entries);

            foreach (var entry in entries)
            {
                var model = ModbusSimServer.GetModelParam(entry.ModelSim!);
                if (model == null) continue;

                RackParamModelLookup[entry.ParamName!] = model;
                if (string.IsNullOrWhiteSpace(model.ModelType) || entry.FunctionCode is 5 or 6 or 16) continue;

                if (!RackModelParamLookup.TryGetValue(model.ModelType, out var list))
                    RackModelParamLookup[model.ModelType] = list = new List<MapEntry>();
                list.Add(entry);
            }

            RackDataMaps.AddRange(entries.Where(m => m.FunctionCode is 3 or 4));
            RackControlMaps.AddRange(entries.Where(m => m.FunctionCode is 5 or 6 or 16));
        }

        /// <summary>
        /// 设备号占位符替换。<paramref name="emuDeviceIdOverride"/> 非空时（LC 聚合组首机组语义），
        /// 无论设备本身是否为 EMU，都把 emuDeviceId 替换为指定机组根路径。
        /// </summary>
        private static void ApplyDeviceIdSubstitution(MapEntry[] entries, string name, bool isEmu, int? emuDeviceIdOverride = null)
        {
            if (!int.TryParse(new string(name.Where(char.IsDigit).ToArray()), out int deviceId))
                return;

            foreach (var e in entries)
            {
                if (e.ModelSim != null)
                {
                    if (!isEmu)
                        e.ModelSim = e.ModelSim.Replace("bmsdeviceId", $"bms{deviceId}", StringComparison.Ordinal);

                    e.ModelSim = e.ModelSim.Replace("pvDeviceId", $"pv{deviceId}", StringComparison.Ordinal);
                    e.ModelSim = e.ModelSim.Replace("deviceId", deviceId.ToString(), StringComparison.Ordinal);

                    if (emuDeviceIdOverride is int emuId)
                        e.ModelSim = e.ModelSim.Replace("emuDeviceId", $"emu{emuId}", StringComparison.Ordinal);
                    else if (isEmu)
                        e.ModelSim = e.ModelSim.Replace("emuDeviceId", $"emu{deviceId}", StringComparison.Ordinal);
                }
            }
        }
    }
}
