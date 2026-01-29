using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using NModbus;
using log4net;
using System.Runtime.CompilerServices;

namespace IEC61850_simulatorServer2
{
    public class MapEntry
    {
        public int Address { get; set; }
        public int FunctionCode { get; set; }
        public string? ParamName { get; set; }
        public int Scale { get; set; }
        public string? Description { get; set; }
        public string? ModelSim { get; set; }
        public int Size { get; set; }
        public string? Type { get; set; }
    }

    public class ModbusSimServer
    {

        IModbusSlave server;
        private ModbusParser parser;
        DeviceInfoDto deviceInfoDto;
        ILog log = LogManager.GetLogger(typeof(ModbusSimServer));
        private readonly List<MapEntry> controlMaps = new List<MapEntry>();
        public readonly List<MapEntry> dataMaps = new List<MapEntry>();
        private readonly List<MapEntry> rackControlMaps = new List<MapEntry>();
        private readonly List<MapEntry> rackDataMaps = new List<MapEntry>();
        public Dictionary<string, ModesimModel> paramModelLookup = new Dictionary<string, ModesimModel>();
        public Dictionary<string, ModesimModel> rackParamModelLookup = new Dictionary<string, ModesimModel>();
        private Thread? syncThread;
        // per-modelType worker threads
        private readonly Dictionary<string, Thread> workerThreads = new Dictionary<string, Thread>();
        private readonly Dictionary<string, List<MapEntry>> modelParamLookup = new Dictionary<string, List<MapEntry>>();
        private readonly Dictionary<string, List<MapEntry>> rackModelParamLookup = new Dictionary<string, List<MapEntry>>();
        private volatile bool _syncRunning = false;
        private readonly Dictionary<string, object?> shadowData = new Dictionary<string, object?>();
        private readonly Dictionary<string, object?> shadowControl = new Dictionary<string, object?>();
        private readonly Dictionary<string, object?>[]? rackShadowData;
        private readonly Dictionary<string, object?>[]? rackShadowControl;
        // simulation state and scheduling
        private readonly Dictionary<string, double> numericState = new Dictionary<string, double>();
        private readonly Dictionary<string, bool> boolState = new Dictionary<string, bool>();
        private readonly Random rnd = new Random();
        private readonly int clusterCount;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mapFilePath">Modbus映射文件路径</param>
        /// <param name="modbusPort">Modbus端口</param>
        /// <param name="serverName">服务器名称</param>
        /// <param name="rackCount">BMS rack数量，目前支持1簇，多簇需要扩展点位表</param>
        public ModbusSimServer(string mapFilePath, int modbusPort, string serverName, int clusterCount = 0)
        {
            this.clusterCount = clusterCount;
            List<MapEntry[]> maps = new List<MapEntry[]>();
            initSimServer(mapFilePath, serverName, ref maps);

            if (serverName.ToLower().Contains("bms"))
            {
                rackShadowData = new Dictionary<string, object?>[clusterCount];
                rackShadowControl = new Dictionary<string, object?>[clusterCount];
                initRackSimServer(mapFilePath, serverName, clusterCount, ref maps);

                for (int i = 0; i < clusterCount; i++)
                {
                    rackShadowData[i] = new Dictionary<string, object?>();
                    foreach (var e in rackDataMaps)
                    {
                        if (!rackShadowData[i].ContainsKey(e.ParamName!)) rackShadowData[i][e.ParamName!] = null;
                    }
                    rackShadowControl[i] = new Dictionary<string, object?>();
                    foreach (var e in rackControlMaps)
                    {
                        if (!rackShadowControl[i].ContainsKey(e.ParamName!)) rackShadowControl[i][e.ParamName!] = null;
                    }
                }
            }

            // 创建 Modbus TCP 从站
            deviceInfoDto = new DeviceInfoDto()
            {
                ip = "0.0.0.0",
                port = modbusPort,
                slaveId = 1,
                connectType = "ModbusTCP",
                collectionCycle = 1000,
                name = serverName
            };
            var tcpSlave = new TCPCommunicator(deviceInfoDto);
            server = new ModbusTCPSlave(deviceInfoDto, maps, tcpSlave, this.clusterCount);
            parser = new ModbusParser(maps);
            Console.WriteLine(serverName + "创建完成");
            Console.WriteLine("ip为" + deviceInfoDto.ip + "端口为" + deviceInfoDto.port);

            // 初始化 shadow 缓存，方便变更检测
            foreach (var e in dataMaps)
            {
                if (!shadowData.ContainsKey(e.ParamName!)) shadowData[e.ParamName!] = 0;
            }
            foreach (var e in controlMaps)
            {
                if (!shadowControl.ContainsKey(e.ParamName!)) shadowControl[e.ParamName!] = 0;
            }
        }

        private Dictionary<string, object> defaultBuffer = new Dictionary<string, object>();
        /// <summary>
        /// 初始化模拟服务器
        /// </summary>
        /// <param name="mapFilePath">映射文件路径</param>
        private void initSimServer(string mapFilePath, string name, ref List<MapEntry[]> maps)
        {
            // 获取点位表
            var tmp = CSVUtil.CSV2Class<MapEntry>(mapFilePath)?.ToArray();
            if (tmp == null) throw new Exception("Modbus map 读取失败");

            // 获取name中数字，如果有的话
            if (int.TryParse(new string(name.Where(char.IsDigit).ToArray()), out int deviceId))
            {
                // 如果有数字的话替换tmp中的ModelSim中的deviceId
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (tmp[i].ModelSim != null)
                    {
                        if (name.Contains("Emu"))
                        {
                            //tmp[i].ModelSim = tmp[i].ModelSim.Replace("deviceId", (deviceId-1).ToString());
                        }
                        else
                        {
                            tmp[i].ModelSim = tmp[i].ModelSim.Replace("deviceId", deviceId.ToString());
                        }
                    }
                }
            }

            maps.Add(tmp);

            // 参数名-》模型 映射关系
            paramModelLookup = new Dictionary<string, ModesimModel>();
            foreach (var entry in tmp)
            {
                ModesimModel? model = GetModelParam(entry.ModelSim!);
                if (model == null)
                {
                    // ModelSim内容转为默认值
                    if (!string.IsNullOrWhiteSpace(entry.ModelSim) && float.TryParse(entry.ModelSim, out var dv))
                    {
                        defaultBuffer[entry.ParamName!] = dv;
                    }
                    continue;
                }
                paramModelLookup[entry.ParamName!] = model;
                if (string.IsNullOrWhiteSpace(model?.ModelType))
                {
                    continue;
                }
                if (6 == entry.FunctionCode)
                {
                    continue;
                }
                if (!modelParamLookup.TryGetValue(model.ModelType, out var list))
                {
                    list = new List<MapEntry>();
                    modelParamLookup[model.ModelType] = list;
                }
                list.Add(entry);
            }
            dataMaps.AddRange(tmp.Where(m => m.FunctionCode == 4 || m.FunctionCode == 3));
            controlMaps.AddRange(tmp.Where(m => m.FunctionCode == 6));
        }

        private void initRackSimServer(string mapFilePath, string serverName, int rackCount, ref List<MapEntry[]> maps)
        {
            // Rack 映射：逐 rack 载入点表，记录模式，默认 slaveId=2，并加入调度
            string rackMapPath = mapFilePath.Replace("bank", "rack");
            var tmp_rack = CSVUtil.CSV2Class<MapEntry>(rackMapPath)?.ToArray();
            if (tmp_rack == null) throw new Exception("Modbus map 读取失败");

            // 获取name中数字，如果有的话
            if (int.TryParse(new string(serverName.Where(char.IsDigit).ToArray()), out int deviceId))
            {
                // 如果有数字的话替换tmp中的ModelSim中的deviceId
                for (int i = 0; i < tmp_rack.Length; i++)
                {
                    if (tmp_rack[i].ModelSim != null)
                    {
                        tmp_rack[i].ModelSim = tmp_rack[i].ModelSim.Replace("deviceId", deviceId.ToString());
                    }
                }
            }

            maps.Add(tmp_rack);
            rackParamModelLookup = new Dictionary<string, ModesimModel>();
            foreach (var entry in tmp_rack)
            {
                ModesimModel? model = GetModelParam(entry.ModelSim!);
                if (model == null) continue;
                rackParamModelLookup[entry.ParamName!] = model;
                if (string.IsNullOrWhiteSpace(model?.ModelType))
                {
                    continue;
                }
                if (3 == entry.FunctionCode)
                {
                    continue;
                }
                if (!rackModelParamLookup.TryGetValue(model.ModelType, out var list))
                {
                    list = new List<MapEntry>();
                    rackModelParamLookup[model.ModelType] = list;
                }
                list.Add(entry);
            }
            rackDataMaps.AddRange(tmp_rack.Where(m => m.FunctionCode == 4 || m.FunctionCode == 3));
            rackControlMaps.AddRange(tmp_rack.Where(m => m.FunctionCode == 6));
        }

        public void Start()
        {
            // 尝试连接设备
            while (true)
            {
                Thread.Sleep(1000);
                if (server != null)
                {
                    server.DeviceConnect();
                    if (server.GetCommunicatorState())
                    {
                        // 设备启动成功，写入默认值
                        server.Write(defaultBuffer);
                        // 启动按 modelType 的后台工作线程
                        StartModelWorkers();
                        // 再启动一个线程主要负责读取modbusslave中yk和yt的值，如果变化了，则将变化的值写到设备模拟器中
                        StartControlThread();
                        break;
                    }
                }
                else
                {
                    log.Error($"Reader is null, DeviceName : {deviceInfoDto.name}");
                    break;
                }
            }
        }

        private int intervalInMs = 1000;
        private void StartControlThread()
        {
            syncThread = new Thread(() =>
            {
                while (_syncRunning)
                {
                    try
                    {
                        if (controlMaps == null || controlMaps.Count == 0)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        // 从 reader 读取原始字节数据（一次性批量读取），然后只选择我们关心的键进行解析
                        var allRaw = server.Read();
                        if (allRaw == null || allRaw.Count == 0)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        var selectedRaw = new Dictionary<string, object>();
                        foreach (var entry in controlMaps)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;
                            if (allRaw.TryGetValue(entry.ParamName, out var raw))
                            {
                                selectedRaw[entry.ParamName] = raw;
                            }
                        }

                        if (selectedRaw.Count == 0)
                        {
                            Thread.Sleep(5000);
                            continue;
                        }

                        // 使用复用的 parser 只解析关心的点
                        var parsed = parser.DataParse(selectedRaw);

                        // 仅在值发生变化时更新模拟器（避免无效写入）
                        foreach (var kv in parsed)
                        {
                            var paramName = kv.Key;
                            var newValue = kv.Value;
                            bool shouldUpdate = false;
                            if (!shadowControl.TryGetValue(paramName, out var prev) || !object.Equals(prev, newValue))
                            {
                                shouldUpdate = true;
                            }
                            if (shouldUpdate)
                            {
                                SetDataObjectByMesurePointName(paramName, newValue);
                                // 无论是否有模式配置，都要同步更新shadowControl，避免每次都触发写入
                                shadowControl[paramName] = newValue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Control thread error", ex);
                    }
                }
            })
            { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            syncThread.Start();
        }

        private void StartModelWorkers()
        {
            if (_syncRunning) return;
            _syncRunning = true;
            // create a worker thread per modelType group
            foreach (var kv in modelParamLookup)
            {
                var modelType = kv.Key;
                var entries = kv.Value;
                var t = new Thread(() => ModelWorkerLoop(modelType, entries)) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                workerThreads[modelType] = t;
                t.Start();
                //// log.Info($"Started worker for modelType={modelType}, count={entries.Count}");
            }

            foreach (var kv in rackModelParamLookup)
            {
                var modelType = kv.Key;
                var entries = kv.Value;
                var t = new Thread(() => ModelWorkerLoopForRack(modelType, entries)) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                workerThreads[modelType + "_rack"] = t;
                t.Start();
                //// log.Info($"Started rack worker for modelType={modelType}, count={entries.Count}");
            }
            //// log.Info("All modelType workers started.");
        }

        public void Stop()
        {
            try
            {
                _syncRunning = false;
                // stop model workers
                foreach (var kv in workerThreads)
                {
                    try { if (kv.Value.IsAlive) kv.Value.Join(2000); } catch { }
                }
                workerThreads.Clear();
                if (syncThread != null && syncThread.IsAlive)
                {
                    syncThread.Join(2000);
                }
            }
            catch (Exception ex)
            {
                log.Error("Stop sync loop error", ex);
            }
            server.DeviceDisconnect();
            // log.Info("ModbusSimServer stopped.");
        }

        // worker loop for a specific modelType group（固定500ms刷新，不再按 nextChange 调度）
        private void ModelWorkerLoop(string modelType, List<MapEntry> entries)
        {
            const int refreshMs = 500;
            if (entries == null || entries.Count == 0) return;

            while (_syncRunning)
            {
                var writeBuffer = new Dictionary<string, object>();
                try
                {
                    foreach (var entry in entries)
                    {
                        if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;

                        var val = GetModelValueStub(entry);
                        if (!shadowData.TryGetValue(entry.ParamName, out var prev) || !object.Equals(prev, val))
                        {
                            shadowData[entry.ParamName] = val;
                            writeBuffer[entry.ParamName] = val;
                        }
                    }

                    if (writeBuffer.Count > 0)
                    {
                        server.Write(writeBuffer);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Worker {modelType} loop error", ex);
                }

                Thread.Sleep(refreshMs);
            }
        }

        // rack 级 worker：固定500ms刷新，不再按 rackNextChange 调度
        private void ModelWorkerLoopForRack(string modelType, List<MapEntry> entries)
        {
            const int refreshMs = 500;
            if (entries == null || entries.Count == 0) return;

            while (_syncRunning)
            {
                try
                {
                    for (int rackId = 0; rackId < clusterCount; rackId++)
                    {
                        var writeBuffer = new Dictionary<string, object>();
                        byte sid = (byte)(deviceInfoDto.slaveId + rackId + 1);
                        foreach (var entry in entries)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;
                            var val = GetModelValueStubForRack(entry, rackId);
                            if (!rackShadowData![rackId].TryGetValue(entry.ParamName, out var prev) || !object.Equals(prev, val))
                            {
                                rackShadowData[rackId][entry.ParamName] = val;
                                writeBuffer[entry.ParamName] = val;
                            }
                        }

                        if (writeBuffer.Count > 0)
                        {
                            server.Write(writeBuffer, sid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Worker {modelType} loop error", ex);
                }

                Thread.Sleep(refreshMs);
            }
        }

        // 临时 stub：根据 paramModelLookup 中的模式生成或读取值（支持 model=1..5）
        private object GetModelValueStub(MapEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) return 0;
            if (!paramModelLookup.TryGetValue(entry.ParamName, out var model)) return 0;

            if (!int.TryParse(model.ModelType, out int modelType))
            {
                // fallback: if modelType not numeric and Arg1 points to model field, try to read that
                var tmp = SimServer.GetExtIfVariableVal(model.Arg1!);
                return tmp ?? 0.0f;
            }

            var now = DateTime.UtcNow;
            switch (modelType)
            {
                // case 1: // 循环增长模式
                //     {
                //         if (!numericState.ContainsKey(entry.ParamName))
                //         {
                //             numericState[entry.ParamName] = ParseDoubleSafe(model.Arg1, 0);
                //             nextChange[entry.ParamName] = now;
                //         }
                //         if (!nextChange.ContainsKey(entry.ParamName) || now >= nextChange[entry.ParamName])
                //         {
                //             double cur = numericState[entry.ParamName];
                //             double step = ParseDoubleSafe(model.Arg3, 1);
                //             double end = ParseDoubleSafe(model.Arg2, cur);
                //             cur += step;
                //             if (cur > end) cur = ParseDoubleSafe(model.Arg1, cur);
                //             numericState[entry.ParamName] = cur;
                //             int interval = ParseIntSafe(model.Arg4, 1000);
                //             nextChange[entry.ParamName] = now.AddMilliseconds(interval);
                //         }
                //         return numericState[entry.ParamName];
                //     }
                // case 2: // 区间随机变位（固定间隔）
                //     {
                //         if (!numericState.ContainsKey(entry.ParamName))
                //         {
                //             numericState[entry.ParamName] = ParseDoubleSafe(model.Arg1, 0);
                //             nextChange[entry.ParamName] = now;
                //         }
                //         if (!nextChange.ContainsKey(entry.ParamName) || now >= nextChange[entry.ParamName])
                //         {
                //             double start = ParseDoubleSafe(model.Arg1, 0);
                //             double end = ParseDoubleSafe(model.Arg2, start);
                //             double val = start + rnd.NextDouble() * (end - start);
                //             numericState[entry.ParamName] = val;
                //             int interval = ParseIntSafe(model.Arg4, 1000);
                //             nextChange[entry.ParamName] = now.AddMilliseconds(interval);
                //         }
                //         return numericState[entry.ParamName];
                //     }
                // case 3: // Bool 固定周期轮替
                //     {
                //         if (!boolState.ContainsKey(entry.ParamName))
                //         {
                //             boolState[entry.ParamName] = false;
                //             nextChange[entry.ParamName] = now;
                //         }
                //         if (!nextChange.ContainsKey(entry.ParamName) || now >= nextChange[entry.ParamName])
                //         {
                //             boolState[entry.ParamName] = !boolState[entry.ParamName];
                //             int interval = ParseIntSafe(model.Arg4, 1000);
                //             nextChange[entry.ParamName] = now.AddMilliseconds(interval);
                //         }
                //         return boolState[entry.ParamName] ? 1 : 0;
                //     }
                case 4: // 关联实时模型字段 — 直接读取 SimServer
                    {
                        var res = SimServer.GetExtIfVariableVal(model.Arg1!);
                        return res ?? 0;
                    }
                // case 5: // Bool 随机区间轮替
                //     {
                //         if (!boolState.ContainsKey(entry.ParamName))
                //         {
                //             boolState[entry.ParamName] = false;
                //             int min = ParseIntSafe(model.Arg3, 1000);
                //             int max = ParseIntSafe(model.Arg4, 5000);
                //             nextChange[entry.ParamName] = now.AddMilliseconds(rnd.Next(Math.Max(1, min), Math.Max(min + 1, max)));
                //         }
                //         if (now >= nextChange[entry.ParamName])
                //         {
                //             boolState[entry.ParamName] = !boolState[entry.ParamName];
                //             int min = ParseIntSafe(model.Arg3, 1000);
                //             int max = ParseIntSafe(model.Arg4, 5000);
                //             int interval = rnd.Next(Math.Max(1, min), Math.Max(min + 1, max));
                //             nextChange[entry.ParamName] = now.AddMilliseconds(interval);
                //         }
                //         return boolState[entry.ParamName] ? 1 : 0;
                //     }
                default:
                    return 0;
            }
        }

        private double ParseDoubleSafe(string? s, double @default)
        {
            if (string.IsNullOrWhiteSpace(s)) return @default;
            if (double.TryParse(s, out var d)) return d;
            return @default;
        }

        private int ParseIntSafe(string? s, int @default)
        {
            if (string.IsNullOrWhiteSpace(s)) return @default;
            if (int.TryParse(s, out var v)) return v;
            if (double.TryParse(s, out var dv)) return (int)dv;
            return @default;
        }

        // Rack 专用：使用 rackParamModelLookup，按 rackId 替换参数并独立维护状态/调度
        private object GetModelValueStubForRack(MapEntry entry, int rackId)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) return 0;
            if (!rackParamModelLookup.TryGetValue(entry.ParamName, out var model)) return 0;

            // 为当前 rack 生成局部参数（不修改原模型）
            string? arg1 = model.Arg1?.Replace("rackId", rackId.ToString());
            // string? arg2 = model.Arg2?.Replace("rackId", rackId.ToString());
            // string? arg3 = model.Arg3?.Replace("rackId", rackId.ToString());
            // string? arg4 = model.Arg4?.Replace("rackId", rackId.ToString());

            if (!int.TryParse(model.ModelType, out int modelType))
            {
                var tmp = SimServer.GetExtIfVariableVal(arg1!);
                return tmp ?? 0;
            }

            // 使用 rack 维度的 key，避免与主设备的状态冲突
            string key = $"{entry.ParamName}#{rackId}";
            var now = DateTime.UtcNow;

            switch (modelType)
            {
                // case 1: // 循环增长模式
                //     {
                //         if (!numericState.ContainsKey(key))
                //         {
                //             numericState[key] = ParseDoubleSafe(arg1, 0);
                //         }
                //         double cur = numericState[key];
                //         double step = ParseDoubleSafe(arg3, 1);
                //         double end = ParseDoubleSafe(arg2, cur);
                //         cur += step;
                //         if (cur > end) cur = ParseDoubleSafe(arg1, cur);
                //         numericState[key] = cur;
                //         return numericState[key];
                //     }
                // case 2: // 区间随机变位（固定间隔）
                //     {
                //         if (!numericState.ContainsKey(key))
                //         {
                //             numericState[key] = ParseDoubleSafe(arg1, 0);
                //         }
                //         double start = ParseDoubleSafe(arg1, 0);
                //         double end = ParseDoubleSafe(arg2, start);
                //         double val = start + rnd.NextDouble() * (end - start);
                //         numericState[key] = val;
                //         return numericState[key];
                //     }
                // case 3: // Bool 固定周期轮替
                //     {
                //         if (!boolState.ContainsKey(key))
                //         {
                //             boolState[key] = false;
                //         }
                //         boolState[key] = !boolState[key];
                //         return boolState[key] ? 1 : 0;
                //     }
                case 4: // 关联实时模型字段 — 直接读取 SimServer
                    {
                        var res = SimServer.GetExtIfVariableVal(arg1!);
                        return res ?? 0;
                    }
                // case 5: // Bool 随机区间轮替
                //     {
                //         if (!boolState.ContainsKey(key))
                //         {
                //             boolState[key] = false;
                //         }
                //         boolState[key] = !boolState[key];
                //         return boolState[key] ? 1 : 0;
                //     }
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 从模型字符串获取模型
        /// </summary>
        /// <param name="modelstring">输入的模型字符串</param>
        /// <returns></returns>
        public static ModesimModel? GetModelParam(string modelstring)
        {
            if (!modelstring.Contains("model")) return null;
            ModesimModel model = new ModesimModel();
            var keyValuePairs = modelstring.Split('|');
            int i = 0;
            foreach (var pair in keyValuePairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0];
                    string value = parts[1].Trim('"');
                    switch (i)
                    {
                        case 0:
                            model.ModelType = value;
                            break;
                        case 1:
                            model.Arg1 = value;
                            break;
                        case 2:
                            model.Arg2 = value;
                            break;
                        case 3:
                            model.Arg3 = value;
                            break;
                        case 4:
                            model.Arg4 = value;
                            break;

                    }
                }
                i++;
            }
            return model;
        }

        public void SetDataObjectByMesurePointName(string mesurePointName, object value)
        {
            if (string.IsNullOrWhiteSpace(mesurePointName)) return;

            // 若该点有模式配置，按类型更新对应的模拟状态
            if (paramModelLookup.TryGetValue(mesurePointName, out var model))
            {
                // 统一转换与记录 shadow
                object valToSet = value;
                if (value is string s)
                {
                    // 尝试解析为数值或布尔
                    if (double.TryParse(s, out var dv)) valToSet = dv;
                    else if (bool.TryParse(s, out var bv)) valToSet = bv ? 1 : 0;
                }

                // control points are written by external Modbus clients; keep them in shadowControl
                shadowControl[mesurePointName] = valToSet;
                SimServer.SetExtIfVariableVal(model.Arg1!, valToSet);
                // log.Info($"{mesurePointName} 数据值更新为：{value}");

                // 可选：立即写入到 Modbus DataStore（按需开启）
                // var buffer = new Dictionary<string, object> { { mesurePointName, shadow[mesurePointName] } };
                // try { reader?.Write(buffer); } catch (Exception ex) { log.Error("即时写入 Modbus 失败", ex); }
            }
        }

        public void SetDataStoreByMesurePointName(string mesurePointName, object value)
        {
            if (string.IsNullOrWhiteSpace(mesurePointName)) return;
            // 可选：立即写入到 Modbus DataStore（按需开启）
            var buffer = new Dictionary<string, object> { { mesurePointName, value } };
            try { server.Write(buffer); } catch (Exception ex) { log.Error("即时写入 Modbus 失败", ex); }
        }

        // 通过 ParamName 获取主要设备的数据对象值
        public object? GetDataObjectByMesurePointName(string mesurePointName)
        {
            if (string.IsNullOrWhiteSpace(mesurePointName)) return null;
            var data = server.Read(mesurePointName);
            if (data != null)
            {
                var parsed = parser.DataParse(new Dictionary<string, object> { { mesurePointName, data } });
                return parsed[mesurePointName];
            }
            else
            {
                return null;
            }
        }
    }
}
