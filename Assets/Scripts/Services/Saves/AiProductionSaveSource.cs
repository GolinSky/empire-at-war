using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// The AI's station level, research, paid builds and unit limits. Restored ships, squadrons not owned by a
    /// hangar bay, and structures not built by a capture site take their limit slots again, as when they were built.
    /// </summary>
    public sealed class AiProductionSaveSource : PlayerSaveSource
    {
        private readonly EnemyFactionModel _faction;
        private readonly FactionResearchModel _research;
        private readonly EnemyFactionController _controller;
        private readonly EnemyUnitLimitModel _limits;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;
        private readonly IProductionAccess _production;

        public AiProductionSaveSource(PlayerSaveRegistry registry, PlayerSlot owner, EnemyFactionModel faction,
            FactionResearchModel research, EnemyFactionController controller, EnemyUnitLimitModel limits,
            IUnitRequestFactory requests, FactionRoster roster, StationLevelData levels, IProductionAccess production)
            : base(registry, owner)
        {
            _production = production;
            _faction = faction;
            _research = research;
            _controller = controller;
            _limits = limits;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public override void Capture(PlayerSave save)
        {
            save.StationLevel = _faction.CurrentLevel;
            save.Research = ResearchSave.CaptureAll(_research);
            save.PendingBuilds = _controller.PendingBuilds
                .Select(build => UnitRequestSave.From(build.Request, build.TimeLeft)).ToArray();
            save.ShipOrders = _limits.ShipOrdersCount;
        }

        public override void RestoreBeforeUnits(PlayerSave save)
        {
            // A hosted ally copies its host's level.
            if (_production.ProductionHost == Owner) _faction.CurrentLevel = save.StationLevel;
            ResearchSave.RestoreAll(save.Research, _research);
            for (int i = 0; i < save.ShipOrders; i++) _limits.RecordShipOrder();
            foreach (UnitRequestSave build in save.PendingBuilds)
                _controller.RestorePendingBuild(build.ToRequest(_requests, _roster, _levels), build.TimeLeft);
        }

        public override void RestoreAfterUnits(PlayerSave save, BattleRestoreContext context)
        {
            foreach (UnitSave unit in context.Save.Units.Where(unit => unit.Owner == Owner))
            {
                if (unit.UnitType.IsShip)
                {
                    var shipType = unit.UnitType.ShipType;
                    _controller.AdoptShip(context.Ships[unit.Id],
                        _requests.ConstructUnitRequest(_roster.Ships[shipType], shipType));
                }
                else if (!context.HangarSquadrons.Contains(unit.Id))
                {
                    var squadronType = unit.UnitType.SquadronType;
                    _controller.AdoptSquadron(context.Squadrons[unit.Id],
                        _requests.ConstructUnitRequest(_roster.Squadrons[squadronType], squadronType));
                }
            }

            foreach (StructureSave structure in context.Save.Structures.Where(structure => structure.Owner == Owner &&
                         structure.Kind != StructureKind.SpaceStation && !context.SiteFacilities.Contains(structure.Id)))
            {
                if (structure.Kind == StructureKind.MiningFacility)
                    _controller.AdoptMiningFacility(context.MiningFacilities[structure.Id], _requests.ConstructUnitRequest(
                        _roster.MiningFacilities[structure.MiningFacilityType], structure.MiningFacilityType));
                else
                    _controller.AdoptDefendPlatform(context.DefendPlatforms[structure.Id], _requests.ConstructUnitRequest(
                        _roster.DefendPlatforms[structure.DefendPlatformType], structure.DefendPlatformType));
            }
        }
    }
}
