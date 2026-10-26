using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public class HealthFacade: IHealthFacade, IHealthDamageFacade
    {
        private readonly IHealthComponent _healthComponent;

        public event Action Damaged;

        public HealthFacade(IHealthComponent healthComponent)
        {
            _healthComponent = healthComponent;
        }

        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            float hull = _healthComponent.HealthModelObserver.Hull;
            float shields = _healthComponent.HealthModelObserver.Shields;
            _healthComponent.ApplyDamage(damage, damageType, id);
            if (_healthComponent.HealthModelObserver.Hull < hull ||
                _healthComponent.HealthModelObserver.Shields < shields)
                Damaged?.Invoke();
        }
    }
}
