using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IHealthCommand : IEntityCommand
    {
        void ApplyDamage(float damage, WeaponType weaponType, int id);
        // todo: add HasCommand<T>: bool
    }
}