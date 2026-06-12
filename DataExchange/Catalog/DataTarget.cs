namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>仿真侧读写目标（rootKey + 属性路径）。</summary>
    public sealed class DataTarget
    {
        public required string RootKey { get; init; }
        public required string PropertyPath { get; init; }

        public string FullPath => string.IsNullOrEmpty(PropertyPath)
            ? RootKey
            : $"{RootKey}.{PropertyPath}";

        public static DataTarget? ParseBindingPath(string? bindingPath)
        {
            if (string.IsNullOrWhiteSpace(bindingPath))
                return null;

            int dot = bindingPath.IndexOf('.');
            if (dot <= 0)
                return new DataTarget { RootKey = bindingPath, PropertyPath = string.Empty };

            return new DataTarget
            {
                RootKey = bindingPath[..dot],
                PropertyPath = bindingPath[(dot + 1)..]
            };
        }
    }
}
