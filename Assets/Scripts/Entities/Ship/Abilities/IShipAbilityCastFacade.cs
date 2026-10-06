using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Entities.Ship.Abilities
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
