using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Ship
{
    /// <summary>
    /// Engine glow follows the ship: off while ion disabled, dim while stopped, full while moving.
    /// Each destroyed engine hardpoint switches off its own glows.
    /// </summary>
    public sealed class ShipEngineGlowSync : IInitializable, ILateDisposable
    {
        private readonly IShipMoveComponent _movement;
        private readonly CombatModifiers _modifiers;
        private readonly IHealthModelObserver _health;
        private readonly IShipEngineGlowComponent _engineGlow;
        private readonly List<(IReadOnlyHardPointModel Engine, Action Handler)> _engines =
            new List<(IReadOnlyHardPointModel, Action)>();

        public ShipEngineGlowSync(IShipMoveComponent movement, CombatModifiers modifiers, IHealthComponent health,
            IShipEngineGlowComponent engineGlow)
        {
            _movement = movement;
            _modifiers = modifiers;
            _health = health.HealthModelObserver;
            _engineGlow = engineGlow;
        }

        public void Initialize()
        {
            _movement.MovingChanged += HandleMovingChanged;
            _modifiers.Changed += Apply;
            // Hardpoint models exist from HealthComponent.Initialize (LifecycleOrders.HEALTH_INITIALIZE), which runs first.
            foreach (IReadOnlyHardPointModel hardPoint in _health.HardPointModels)
            {
                if (hardPoint.HardPointType != HardPointType.Engines) continue;
                IReadOnlyHardPointModel engine = hardPoint;
                Action handler = () => _engineGlow.SetEngineDestroyed(engine.Id, engine.IsDestroyed);
                engine.OnHardPointHealthChanged += handler;
                _engines.Add((engine, handler));
                handler();
            }

            Apply();
        }

        public void LateDispose()
        {
            _movement.MovingChanged -= HandleMovingChanged;
            _modifiers.Changed -= Apply;
            foreach ((IReadOnlyHardPointModel engine, Action handler) in _engines) engine.OnHardPointHealthChanged -= handler;
            _engines.Clear();
        }

        private void HandleMovingChanged(bool isMoving) => Apply();

        private void Apply() => _engineGlow.SetState(_modifiers.IsIonDisabled ? EngineGlowState.Off
            : _movement.IsMoving ? EngineGlowState.Thrust : EngineGlowState.Idle);
    }
}
