using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public class HealthFacade: IHealthFacade, IHealthDamageFacade
    {
        private readonly IHealthComponent _healthComponent;
        private readonly IMatchSession _session;

        public event Action Damaged;

        public HealthFacade(IHealthComponent healthComponent, IMatchSession session)
        {
            _healthComponent = healthComponent;
            _session = session;
        }

        // Online clients replay abilities and super weapons for their effects; only the host deals damage.
        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            if (!_session.IsAuthority) return;
            float hull = _healthComponent.HealthModelObserver.Hull;
            float shields = _healthComponent.HealthModelObserver.Shields;
            _healthComponent.ApplyDamage(damage, damageType, id);
            if (_healthComponent.HealthModelObserver.Hull < hull ||
                _healthComponent.HealthModelObserver.Shields < shields)
                Damaged?.Invoke();
        }
    }
}
