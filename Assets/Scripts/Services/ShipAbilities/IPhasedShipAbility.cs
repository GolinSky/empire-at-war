using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IPhasedShipAbility : IShipAbility
    {
        bool IsComplete { get; }
        bool SurvivesCasterDeath { get; }
        bool CanStart(IShipAbilityFacade caster, IEntity target);
        void Advance(float deltaTime);
    }
}
