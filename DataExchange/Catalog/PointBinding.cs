namespace EssSimulator.DataExchange.Catalog
{
    public sealed class PointBinding
    {
        public required MapEntry Entry { get; init; }
        public required string ParamName { get; init; }
        public required DataTarget Target { get; init; }
        public ControlSemantics Semantics { get; init; } = ControlSemantics.Hold;
        public ControlEffectId Effect { get; init; } = ControlEffectId.None;
        public bool IsTelemetry => Entry.FunctionCode is 3 or 4;
        public bool IsControl => Entry.FunctionCode is 5 or 6 or 16;
    }
}
