using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
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
        public HarmonicBombService HarmonicBombs { get; }
        public IVisionService Vision { get; }
        public IMatchSession Session { get; }
        public IBattleAreaWriter BattleAreas { get; }
        public GravityWellEffects GravityWells { get; }

        public ShipAbilityDependencies(IEntityLocator entities, IPlayerRelations relations,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData, BoltFlightBatch boltFlights,
            HarmonicBombService harmonicBombs, IVisionService vision, IMatchSession session,
            IBattleAreaWriter battleAreas, GravityWellEffects gravityWells)
        {
            Entities = entities;
            Relations = relations;
            ImpactPresenter = impactPresenter;
            WeaponsData = weaponsData;
            BoltFlights = boltFlights;
            HarmonicBombs = harmonicBombs;
            Vision = vision;
            Session = session;
            BattleAreas = battleAreas;
            GravityWells = gravityWells;
        }
    }
}
