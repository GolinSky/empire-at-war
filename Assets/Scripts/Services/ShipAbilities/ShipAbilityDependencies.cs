using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.ViewComponents.Weapon;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityDependencies
    {
        public IEntityLocator Entities { get; }
        public IPlayerRelations Relations { get; }
        public ImpactEffectPresenter ImpactPresenter { get; }
        public WeaponsData WeaponsData { get; }
        public BoltFlightBatch BoltFlights { get; }

        public ShipAbilityDependencies(IEntityLocator entities, IPlayerRelations relations,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData, BoltFlightBatch boltFlights)
        {
            Entities = entities;
            Relations = relations;
            ImpactPresenter = impactPresenter;
            WeaponsData = weaponsData;
            BoltFlights = boltFlights;
        }
    }
}
