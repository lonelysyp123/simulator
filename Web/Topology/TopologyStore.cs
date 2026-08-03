using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;

namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 组态工程与设备库的 JSON 文件持久化。
    /// 目录：{ContentRoot}/configs/topology/
    ///   project.json           — 当前画布工程
    ///   library/{id}.json      — 基于模板改参后的可复用设备
    /// </summary>
    public sealed class TopologyStore
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { new ObjectJsonConverter() }
        };

        private readonly object _gate = new();
        private readonly string _rootDir;
        private readonly string _projectPath;
        private readonly string _libraryDir;

        public TopologyStore(IWebHostEnvironment env)
        {
            _rootDir = Path.Combine(env.ContentRootPath, "configs", "topology");
            _projectPath = Path.Combine(_rootDir, "project.json");
            _libraryDir = Path.Combine(_rootDir, "library");
            Directory.CreateDirectory(_libraryDir);
        }

        public string RootDirectory => _rootDir;

        public TopologyProject LoadProject()
        {
            lock (_gate)
            {
                if (!File.Exists(_projectPath))
                    return new TopologyProject();

                try
                {
                    var json = File.ReadAllText(_projectPath);
                    var project = JsonSerializer.Deserialize<TopologyProject>(json, JsonOpts) ?? new TopologyProject();
                    TopologyValidator.RefreshAcBusEnergization(project);
                    return project;
                }
                catch
                {
                    return new TopologyProject();
                }
            }
        }

        public TopologyProject SaveProject(TopologyProject project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            lock (_gate)
            {
                Directory.CreateDirectory(_rootDir);
                if (string.IsNullOrWhiteSpace(project.Id))
                    project.Id = "current";
                NormalizeParameters(project);
                project.UpdatedAtUtc = DateTime.UtcNow;
                TopologyValidator.RefreshAcBusEnergization(project);
                var json = JsonSerializer.Serialize(project, JsonOpts);
                File.WriteAllText(_projectPath, json);
                return project;
            }
        }

        private static void NormalizeParameters(TopologyProject project)
        {
            foreach (var node in project.Nodes)
                node.Parameters = NormalizeDict(node.Parameters);
        }

        private static Dictionary<string, object?> NormalizeDict(Dictionary<string, object?>? src)
        {
            var dst = new Dictionary<string, object?>();
            if (src == null) return dst;
            foreach (var kv in src)
                dst[kv.Key] = NormalizeValue(kv.Value);
            return dst;
        }

        private static object? NormalizeValue(object? raw)
        {
            if (raw is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.String => je.GetString(),
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                    JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText(), JsonOpts),
                    JsonValueKind.Array => JsonSerializer.Deserialize<List<object?>>(je.GetRawText(), JsonOpts),
                    _ => je.ToString()
                };
            }
            return raw;
        }

        public IReadOnlyList<TopologyLibraryItem> ListLibrary()
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_libraryDir);
                var list = new List<TopologyLibraryItem>();
                foreach (var path in Directory.EnumerateFiles(_libraryDir, "*.json"))
                {
                    try
                    {
                        var item = JsonSerializer.Deserialize<TopologyLibraryItem>(File.ReadAllText(path), JsonOpts);
                        if (item != null) list.Add(item);
                    }
                    catch
                    {
                        /* skip corrupt */
                    }
                }

                return list.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        public TopologyLibraryItem? GetLibraryItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            lock (_gate)
            {
                var path = LibraryPath(id);
                if (!File.Exists(path)) return null;
                try
                {
                    return JsonSerializer.Deserialize<TopologyLibraryItem>(File.ReadAllText(path), JsonOpts);
                }
                catch
                {
                    return null;
                }
            }
        }

        public TopologyLibraryItem SaveLibraryItem(TopologyLibraryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(item.TemplateId))
                throw new ArgumentException("TemplateId 不能为空", nameof(item));
            if (TopologyTemplates.Get(item.TemplateId) == null)
                throw new ArgumentException($"未知模板: {item.TemplateId}", nameof(item));

            lock (_gate)
            {
                Directory.CreateDirectory(_libraryDir);
                if (string.IsNullOrWhiteSpace(item.Id))
                    item.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(item.Name))
                    item.Name = "未命名设备";
                item.Parameters = NormalizeDict(item.Parameters);
                item.UpdatedAtUtc = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(item, JsonOpts);
                File.WriteAllText(LibraryPath(item.Id), json);
                return item;
            }
        }

        public bool DeleteLibraryItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            lock (_gate)
            {
                var path = LibraryPath(id);
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
        }

        private string LibraryPath(string id)
        {
            var safe = Path.GetFileName(id);
            return Path.Combine(_libraryDir, safe + ".json");
        }
    }

    /// <summary>
    /// 将 Dictionary&lt;string, object?&gt; 反序列化为可读写的标量/嵌套结构，避免裸 JsonElement 难以处理。
    /// </summary>
    internal sealed class ObjectJsonConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadValue(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    writer.WriteNumberValue(Convert.ToInt64(value));
                    break;
                case float f:
                    writer.WriteNumberValue(f);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case decimal m:
                    writer.WriteNumberValue(m);
                    break;
                case Dictionary<string, object?> dict:
                    writer.WriteStartObject();
                    foreach (var kv in dict)
                    {
                        writer.WritePropertyName(kv.Key);
                        Write(writer, kv.Value, options);
                    }
                    writer.WriteEndObject();
                    break;
                case IEnumerable<object?> list when value is not string:
                    writer.WriteStartArray();
                    foreach (var item in list)
                        Write(writer, item, options);
                    writer.WriteEndArray();
                    break;
                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }

        private static object? ReadValue(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.Null: return null;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l)) return l;
                    return reader.GetDouble();
                case JsonTokenType.StartObject:
                {
                    var dict = new Dictionary<string, object?>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        var key = reader.GetString()!;
                        reader.Read();
                        dict[key] = ReadValue(ref reader);
                    }
                    return dict;
                }
                case JsonTokenType.StartArray:
                {
                    var list = new List<object?>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        list.Add(ReadValue(ref reader));
                    return list;
                }
                default:
                    return null;
            }
        }
    }
}
