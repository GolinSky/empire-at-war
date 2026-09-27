using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IHealthFacade : IEntityFacade
    {
        void ApplyDamage(float damage, DamageType damageType, int id);
        // todo: add HasCommand<T>: bool
    }
}