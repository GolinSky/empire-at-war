using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Ship
{
    /// <summary>Engine glow follows the ship: off while ion disabled, dim while stopped, full while moving.</summary>
    public sealed class ShipEngineGlowSync : IInitializable, ILateDisposable
    {
        private readonly IShipMoveComponent _movement;
        private readonly CombatModifiers _modifiers;
        private readonly IShipEngineGlowComponent _engineGlow;

        public ShipEngineGlowSync(IShipMoveComponent movement, CombatModifiers modifiers, IShipEngineGlowComponent engineGlow)
        {
            _movement = movement;
            _modifiers = modifiers;
            _engineGlow = engineGlow;
        }

        public void Initialize()
        {
            _movement.MovingChanged += HandleMovingChanged;
            _modifiers.Changed += Apply;
            Apply();
        }

        public void LateDispose()
        {
            _movement.MovingChanged -= HandleMovingChanged;
            _modifiers.Changed -= Apply;
        }

        private void HandleMovingChanged(bool isMoving) => Apply();

        private void Apply() => _engineGlow.SetState(_modifiers.IsIonDisabled ? EngineGlowState.Off
            : _movement.IsMoving ? EngineGlowState.Thrust : EngineGlowState.Idle);
    }
}
