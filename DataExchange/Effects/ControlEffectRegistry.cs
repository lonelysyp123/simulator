using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.DataExchange.Effects
{
    public sealed class ControlEffectRegistry
    {
        private readonly Dictionary<ControlEffectId, IControlEffect> _effects = new();

        public ControlEffectRegistry Register(IControlEffect effect)
        {
            _effects[effect.Id] = effect;
            return this;
        }

        public void Dispatch(ControlEffectId effectId, ControlEffectContext context)
        {
            if (effectId == ControlEffectId.None)
                return;

            if (_effects.TryGetValue(effectId, out var effect))
                effect.OnControlChanged(context);
        }
    }
}
