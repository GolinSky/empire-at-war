using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbility
    {
        void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target);

        void Stop();
    }
}
