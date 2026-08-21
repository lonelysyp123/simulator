using System;
using System.IO;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>
    /// 解析运行时点表 CSV 路径：优先按设备型号选型（pointmaps/models/{type}/{model}），
    /// 其次工作目录/输出目录根下的文件，
    /// 最后 <c>pointmaps/common/</c>（未执行 sync-pointmaps-to-root 时的开发兜底）。
    /// </summary>
    public static class PointMapPathResolver
    {
        public static string Resolve(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("点表文件名不能为空", nameof(fileName));

            if (Path.IsPathRooted(fileName) && File.Exists(fileName))
                return fileName;

            // 设备型号选型命中时优先使用型号目录下的点表
            var selected = ResolveSelected(fileName);
            if (selected != null)
                return selected;

            string name = Path.GetFileName(fileName);
            string[] bases =
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var root in bases)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                var direct = Path.Combine(root, name);
                if (File.Exists(direct)) return Path.GetFullPath(direct);

                var common = Path.Combine(root, "pointmaps", "common", name);
                if (File.Exists(common)) return Path.GetFullPath(common);

                // 若根目录已有 pointmap-version / 其他版本目录，尝试 common 以外的现成副本
                var pointmaps = Path.Combine(root, "pointmaps");
                if (Directory.Exists(pointmaps))
                {
                    foreach (var dir in Directory.EnumerateDirectories(pointmaps))
                    {
                        var candidate = Path.Combine(dir, name);
                        if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                    }
                }
            }

            throw new FileNotFoundException(
                $"找不到点表文件 `{name}`。请在仓库根目录执行: ./scripts/sync-pointmaps-to-root.sh",
                name);
        }

        /// <summary>按设备型号选型解析点表；未选型、类型未声明该文件或型号目录缺文件时返回 null。</summary>
        private static string? ResolveSelected(string fileName)
        {
            var typeId = DeviceModelRegistry.FindTypeForFile(fileName);
            if (typeId == null) return null;

            var dir = DeviceModelRegistry.GetSelectedModelDir(typeId, fileName);
            if (dir == null) return null;

            var candidate = Path.Combine(dir, Path.GetFileName(fileName));
            return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
        }

        /// <summary>相对某已解析点表，查找同目录或回退 Resolve 的伴随文件（如 bms_rack.csv）。</summary>
        public static string ResolveSibling(string alreadyResolvedMapPath, string siblingFileName)
        {
            if (!string.IsNullOrWhiteSpace(alreadyResolvedMapPath))
            {
                var dir = Path.GetDirectoryName(alreadyResolvedMapPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    var sibling = Path.Combine(dir, Path.GetFileName(siblingFileName));
                    if (File.Exists(sibling)) return sibling;
                }
            }
            return Resolve(siblingFileName);
        }
    }
}
