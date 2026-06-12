using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.DataExchange.Effects
{
    public sealed class ControlEffectContext
    {
        public required string ServerName { get; init; }
        public required PointBinding Binding { get; init; }
        public required object AppliedValue { get; init; }
        public required object? PreviousValue { get; init; }
    }

    public interface IControlEffect
    {
        ControlEffectId Id { get; }
        void OnControlChanged(ControlEffectContext context);
    }
}
