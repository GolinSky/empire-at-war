using EmpireAtWar.Components.Combat;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.TeamColor
{
    /// <summary>Shows electricity on the hull (ShipIonStunPass.hlsl) while the unit is ion disabled.</summary>
    public sealed class UnitIonStunSync : IInitializable, ILateDisposable
    {
        private readonly CombatModifiers _modifiers;
        private readonly IUnitRenderStateComponent _renderState;

        public UnitIonStunSync(CombatModifiers modifiers, IUnitRenderStateComponent renderState)
        {
            _modifiers = modifiers;
            _renderState = renderState;
        }

        public void Initialize()
        {
            _modifiers.Changed += HandleModifiersChanged;
            HandleModifiersChanged();
        }

        public void LateDispose()
        {
            _modifiers.Changed -= HandleModifiersChanged;
        }

        private void HandleModifiersChanged() => _renderState.SetIonStunned(_modifiers.IsIonDisabled);
    }
}
