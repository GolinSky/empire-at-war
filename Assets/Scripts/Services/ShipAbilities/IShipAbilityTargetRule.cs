using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityTargetRule
    {
        bool CanTarget(IShipAbilityFacade caster, IEntity target);
    }
}
