using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbility
    {
        void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target);
        void Stop();
    }
}
