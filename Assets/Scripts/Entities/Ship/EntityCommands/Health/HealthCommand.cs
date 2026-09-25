using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Health
{
    public class HealthCommand: IHealthCommand
    {
        private readonly IHealthComponent _healthComponent;

        public HealthCommand(IHealthComponent healthComponent)
        {
            _healthComponent = healthComponent;
        }
        
        public void ApplyDamage(float damage, DamageType damageType, int id)
        {
            _healthComponent.ApplyDamage(damage, damageType, id);
        }
    }
}