using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IHealthCommand : IEntityCommand
    {
        void ApplyDamage(float damage, DamageType damageType, int id);
        // todo: add HasCommand<T>: bool
    }
}