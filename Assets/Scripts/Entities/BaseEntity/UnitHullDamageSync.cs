using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>
    /// Shows the unit's hull loss on its hull (ShipHullDamage.hlsl). Follows repairs and pooled respawns too,
    /// because every hull change raises <see cref="IHealthModelObserver.OnValueChanged"/>.
    /// </summary>
    public sealed class UnitHullDamageSync : IInitializable, ILateDisposable
    {
        private readonly IHealthModelObserver _health;
        private readonly IUnitRenderStateComponent _renderState;

        public UnitHullDamageSync(IHealthComponent healthComponent, IUnitRenderStateComponent renderState)
        {
            _health = healthComponent.HealthModelObserver;
            _renderState = renderState;
        }

        public void Initialize()
        {
            _health.OnValueChanged += HandleHealthChanged;
            HandleHealthChanged();
        }

        public void LateDispose()
        {
            _health.OnValueChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged() => _renderState.SetHullDamage(_health.HullPercentage);
    }
}
