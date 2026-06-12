namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>BMS Rack 级点绑定（Arg1 含 rackId 占位符）。</summary>
    public sealed class RackPointBinding
    {
        public required MapEntry Entry { get; init; }
        public required string ParamName { get; init; }
        public required string BindingPathTemplate { get; init; }

        public string ResolvePath(int rackIndex) =>
            BindingPathTemplate.Replace("rackId", rackIndex.ToString(), StringComparison.Ordinal);
    }
}
