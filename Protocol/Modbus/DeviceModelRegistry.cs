using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>设备类型元数据（pointmaps/models/{type}/type.json）。</summary>
    public sealed class DeviceModelTypeInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>该类型设备使用的点表文件名（如 bms_bank.csv / bms_rack.csv）。</summary>
        public List<string> Files { get; set; } = new();
        /// <summary>该类型下的设备型号（由注册表扫描填充，不来自 type.json）。</summary>
        public List<DeviceModelInfo> Models { get; set; } = new();
    }

    /// <summary>设备型号元数据（pointmaps/models/{type}/{model}/model.json）。</summary>
    public sealed class DeviceModelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>型号点表所在目录（绝对路径）。</summary>
        [JsonIgnore]
        public string Directory { get; set; } = string.Empty;
    }

    /// <summary>设备型号选型持久化内容（configs/topology/device-models.json）。</summary>
    public sealed class DeviceModelSelection
    {
        /// <summary>设备类型 id → 型号 id。</summary>
        public Dictionary<string, string> Selections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTime UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// 设备型号注册表：扫描 <c>pointmaps/models/{type}/{model}/</c> 发现设备类型与型号，
    /// 并在 <c>configs/topology/device-models.json</c> 持久化当前选型。
    /// 选型命中时 <see cref="PointMapPathResolver"/> 优先从型号目录解析点表；
    /// 无选型文件时保持历史行为（根目录固定文件名 + pointmaps 版本目录兜底）。
    /// </summary>
    public static class DeviceModelRegistry
    {
        public const string ModelsRelativeDir = "pointmaps/models";
        public const string SelectionRelativePath = "configs/topology/device-models.json";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private static readonly object Gate = new();
        private static DeviceModelSelection? _cachedSelection;

        /// <summary>候选根目录：工作目录与程序输出目录（与点表解析保持一致）。</summary>
        public static IReadOnlyList<string> CandidateRoots()
        {
            var roots = new List<string>();
            var cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(cwd)) roots.Add(cwd);
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir) && !roots.Contains(baseDir, StringComparer.Ordinal))
                roots.Add(baseDir);
            return roots;
        }

        /// <summary>第一个存在 <c>pointmaps/models</c> 的候选根目录；都不存在时返回 null。</summary>
        public static string? FindModelsRoot()
        {
            foreach (var root in CandidateRoots())
            {
                if (Directory.Exists(Path.Combine(root, ModelsRelativeDir)))
                    return root;
            }
            return null;
        }

        /// <summary>选型文件目录：第一个存在 <c>configs/topology</c> 的候选根，否则程序输出目录。</summary>
        public static string FindSelectionRoot()
        {
            foreach (var root in CandidateRoots())
            {
                if (Directory.Exists(Path.Combine(root, "configs", "topology")))
                    return root;
            }
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// 扫描所有设备类型及其型号。type.json 缺失或损坏时回退为目录名；
        /// 型号目录缺失 model.json 时以目录名兜底。
        /// </summary>
        public static List<DeviceModelTypeInfo> ListTypes(string? rootOverride = null)
        {
            var root = rootOverride ?? FindModelsRoot();
            var result = new List<DeviceModelTypeInfo>();
            if (root == null) return result;

            var modelsRoot = Path.Combine(root, ModelsRelativeDir);
            foreach (var typeDir in Directory.EnumerateDirectories(modelsRoot).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var typeId = Path.GetFileName(typeDir);
                var type = ReadJsonOrDefault<DeviceModelTypeInfo>(Path.Combine(typeDir, "type.json"))
                           ?? new DeviceModelTypeInfo();
                if (string.IsNullOrWhiteSpace(type.Id)) type.Id = typeId;
                if (string.IsNullOrWhiteSpace(type.Name)) type.Name = typeId;
                type.Models = ListModels(typeDir);
                result.Add(type);
            }
            return result;
        }

        /// <summary>扫描某设备类型目录下的所有型号。</summary>
        public static List<DeviceModelInfo> ListModels(string typeDir)
        {
            var models = new List<DeviceModelInfo>();
            if (!Directory.Exists(typeDir)) return models;

            foreach (var modelDir in Directory.EnumerateDirectories(typeDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var modelId = Path.GetFileName(modelDir);
                var model = ReadJsonOrDefault<DeviceModelInfo>(Path.Combine(modelDir, "model.json"))
                            ?? new DeviceModelInfo();
                if (string.IsNullOrWhiteSpace(model.Id)) model.Id = modelId;
                if (string.IsNullOrWhiteSpace(model.Name)) model.Name = modelId;
                model.Directory = modelDir;
                models.Add(model);
            }
            return models;
        }

        /// <summary>点表文件名 → 设备类型 id（按 type.json 的 files 声明匹配）；无匹配返回 null。</summary>
        public static string? FindTypeForFile(string fileName, string? rootOverride = null)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            var name = Path.GetFileName(fileName);
            foreach (var type in ListTypes(rootOverride))
            {
                if (type.Files.Any(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                    return type.Id;
            }
            return null;
        }

        /// <summary>某类型当前选中的型号目录；未选型或目录缺失返回 null。</summary>
        public static string? GetSelectedModelDir(string typeId, string fileName, string? rootOverride = null)
        {
            var selection = LoadSelection(rootOverride);
            if (!selection.Selections.TryGetValue(typeId, out var modelId) || string.IsNullOrWhiteSpace(modelId))
                return null;

            var root = rootOverride ?? FindModelsRoot();
            if (root == null) return null;

            var dir = Path.Combine(root, ModelsRelativeDir, typeId, modelId);
            if (!Directory.Exists(dir)) return null;
            return dir;
        }

        /// <summary>读取选型（带内存缓存；进程启动后选型仅经重启生效）。</summary>
        public static DeviceModelSelection LoadSelection(string? rootOverride = null)
        {
            lock (Gate)
            {
                if (_cachedSelection != null && rootOverride == null)
                    return _cachedSelection;

                var path = SelectionFilePath(rootOverride);
                var selection = File.Exists(path) ? ReadJsonOrDefault<DeviceModelSelection>(path) : null;
                selection ??= new DeviceModelSelection();

                if (rootOverride == null)
                    _cachedSelection = selection;
                return selection;
            }
        }

        /// <summary>保存选型并刷新缓存。</summary>
        public static DeviceModelSelection SaveSelection(DeviceModelSelection selection, string? rootOverride = null)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            lock (Gate)
            {
                var path = SelectionFilePath(rootOverride);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                selection.UpdatedAtUtc = DateTime.UtcNow;
                File.WriteAllText(path, JsonSerializer.Serialize(selection, JsonOpts));
                if (rootOverride == null)
                    _cachedSelection = selection;
                return selection;
            }
        }

        /// <summary>清除内存缓存（测试用）。</summary>
        public static void InvalidateCache()
        {
            lock (Gate)
            {
                _cachedSelection = null;
            }
        }

        /// <summary>校验选型合法性：类型与型号必须存在。返回错误列表（空 = 通过）。</summary>
        public static List<string> ValidateSelection(Dictionary<string, string> selections, string? rootOverride = null)
        {
            var errors = new List<string>();
            if (selections == null || selections.Count == 0)
            {
                errors.Add("选型内容为空");
                return errors;
            }

            var types = ListTypes(rootOverride).ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in selections)
            {
                if (!types.TryGetValue(pair.Key, out var type))
                {
                    errors.Add($"未知设备类型: {pair.Key}");
                    continue;
                }
                if (!type.Models.Any(m => string.Equals(m.Id, pair.Value, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"设备类型 [{pair.Key}] 下不存在型号: {pair.Value}");
            }
            return errors;
        }

        public static string SelectionFilePath(string? rootOverride = null)
        {
            var root = rootOverride ?? FindSelectionRoot();
            return Path.Combine(root, SelectionRelativePath);
        }

        private static T? ReadJsonOrDefault<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOpts);
            }
            catch
            {
                return null;
            }
        }
    }
}
