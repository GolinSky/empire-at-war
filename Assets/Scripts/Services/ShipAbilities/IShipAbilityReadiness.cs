using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityReadiness
    {
        bool IsReady(IShipAbilityFacade caster);
    }
}
