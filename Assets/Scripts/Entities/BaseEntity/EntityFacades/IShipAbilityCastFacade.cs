using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>
    /// Orders a unit to close to the ability range of the target and then use the ability.
    /// Any later order replaces the pending cast.
    /// </summary>
    public interface IShipAbilityCastFacade : IEntityFacade
    {
        void CastAbility(ShipAbilityId id, IEntity target, float range);
    }
}
