using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// The human's built units waiting to be placed, and the bookkeeping of placed ones: restored ships and
    /// squadrons count against unit capacity again, placed structures against their structure limit.
    /// Hangar squadrons and capture-site facilities are not counted, as when they were first built.
    /// </summary>
    public sealed class ReinforcementSaveSource : PlayerSaveSource
    {
        private readonly ReinforcementModel _reinforcements;
        private readonly ReinforcementService _reinforcementService;
        private readonly PlayerFactionModel _faction;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;

        public ReinforcementSaveSource(PlayerSaveRegistry registry, PlayerSlot owner, ReinforcementModel reinforcements,
            ReinforcementService reinforcementService, PlayerFactionModel faction, IUnitRequestFactory requests,
            FactionRoster roster, StationLevelData levels)
            : base(registry, owner)
        {
            _reinforcements = reinforcements;
            _reinforcementService = reinforcementService;
            _faction = faction;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public override void Capture(PlayerSave save) => save.Reserve = _reinforcements.Reserve
            .SelectMany(entry => Enumerable.Repeat(UnitRequestSave.From(entry.Key), entry.Value)).ToArray();

        public override void RestoreBeforeUnits(PlayerSave save)
        {
            foreach (UnitRequestSave reserved in save.Reserve)
            {
                UnitRequest request = reserved.ToRequest(_requests, _roster, _levels);
                // A built structure keeps its limit slot while it waits to be placed.
                if (request is MiningFacilityUnitRequest || request is DefendPlatformUnitRequest)
                    _faction.ReserveStructure(request);
                // So does a built Resolute: only one may be queued, in reserve or deployed.
                if (request is ShipUnitRequest ship && ship.Key == ShipType.Resolute)
                    _faction.SetResoluteReserved(true);
                _reinforcementService.Add(request);
            }
        }

        public override void RestoreAfterUnits(PlayerSave save, BattleRestoreContext context)
        {
            foreach (UnitSave unit in context.Save.Units.Where(unit => unit.Owner == Owner))
            {
                if (unit.UnitType.IsShip)
                {
                    _reinforcements.UpdateShipData(_requests.ConstructUnitRequest(
                        _roster.Ships[unit.UnitType.ShipType], unit.UnitType.ShipType));
                    _reinforcements.AddUnitCapacity(unit.UnitType.ShipType);
                    context.Ships[unit.Id].OnRelease += _reinforcements.RemoveUnitCapacity;
                }
                else if (!context.HangarSquadrons.Contains(unit.Id))
                {
                    var squadronType = unit.UnitType.SquadronType;
                    _reinforcements.UpdateSquadronData(_requests.ConstructUnitRequest(
                        _roster.Squadrons[squadronType], squadronType));
                    _reinforcements.AddUnitCapacity(squadronType);
                    context.Squadrons[unit.Id].Released += () => _reinforcements.RemoveUnitCapacity(squadronType);
                }
            }

            foreach (StructureSave structure in OwnedPlacedStructures(context))
            {
                if (structure.Kind == StructureKind.MiningFacility)
                {
                    MiningFacilityType type = structure.MiningFacilityType;
                    _faction.ReserveStructure(_requests.ConstructUnitRequest(_roster.MiningFacilities[type], type));
                    context.MiningFacilities[structure.Id].OnRelease += () =>
                        _faction.ReleaseStructure<MiningFacilityUnitRequest>(type.ToString());
                }
                else
                {
                    DefendPlatformType type = structure.DefendPlatformType;
                    _faction.ReserveStructure(_requests.ConstructUnitRequest(_roster.DefendPlatforms[type], type));
                    context.DefendPlatforms[structure.Id].OnRelease += () =>
                        _faction.ReleaseStructure<DefendPlatformUnitRequest>(type.ToString());
                }
            }
        }

        private IEnumerable<StructureSave> OwnedPlacedStructures(BattleRestoreContext context) =>
            context.Save.Structures.Where(structure => structure.Owner == Owner &&
                structure.Kind != StructureKind.SpaceStation && !context.SiteFacilities.Contains(structure.Id));
    }
}
