using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Health
{
    public class HealthFacade: IHealthFacade
    {
        private readonly IHealthComponent _healthComponent;

        public HealthFacade(IHealthComponent healthComponent)
        {
            _healthComponent = healthComponent;
        }
        
        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            _healthComponent.ApplyDamage(damage, damageType, id);
        }
    }
}