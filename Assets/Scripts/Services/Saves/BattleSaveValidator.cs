using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ship;
using ShipEntity = EmpireAtWar.Ship.Ship;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Verifies a save against current game data and map counts before loading the battle scene.</summary>
    public sealed class BattleSaveValidator
    {
        private readonly IAssetService _assets;
        private readonly MapGenerationSettings _mapSettings;

        public BattleSaveValidator(IAssetService assets, MapGenerationSettings mapSettings = null)
        {
            _assets = assets;
            _mapSettings = mapSettings;
        }

        public void Validate(BattleSave save, BattleDefinition definition)
        {
            ValidateMapCounts(save, definition);
            ValidateSurvival(save, definition);

            FactionCatalog factions = _assets.Load<FactionCatalog>(nameof(FactionCatalog));
            MiningFacilityCatalog mines = _assets.Load<MiningFacilityCatalog>(nameof(MiningFacilityCatalog));
            DefendPlatformCatalog platforms = _assets.Load<DefendPlatformCatalog>(nameof(DefendPlatformCatalog));
            SuperWeaponCatalog superWeapons = _assets.Load<SuperWeaponCatalog>(nameof(SuperWeaponCatalog));
            StationLevelData levels = _assets.Load<StationLevelData>(nameof(StationLevelData));
            ShipsData shipsData = _assets.Load<ShipsData>(nameof(ShipsData));

            var rosters = new Dictionary<PlayerId, FactionRoster>();
            var players = new Dictionary<PlayerId, BattlePlayer>();
            foreach (BattlePlayer player in definition.Players)
            {
                players.Add(player.Slot.Id, player);
                if (player.Has(BattleFeatures.Economy))
                    rosters.Add(player.Slot.Id, new FactionRoster(
                        factions.Get(player.Slot.Faction), mines, platforms, superWeapons));
            }

            var entityIds = new HashSet<long>();
            var facilityIds = new HashSet<long>();
            var squadrons = new Dictionary<long, UnitSave>();
            foreach (StructureSave structure in save.Structures)
            {
                if (!entityIds.Add(structure.Id)) throw new InvalidDataException($"Duplicate entity id {structure.Id}.");
                if (structure.Kind != StructureKind.SpaceStation) facilityIds.Add(structure.Id);
            }
            foreach (UnitSave unit in save.Units)
            {
                if (!entityIds.Add(unit.Id)) throw new InvalidDataException($"Duplicate entity id {unit.Id}.");
                if (!unit.UnitType.IsShip) squadrons.Add(unit.Id, unit);
            }

            var siteFacilities = ValidateSitesAndZones(save, players, facilityIds);
            var adoptedSquadrons = new HashSet<long>();
            foreach (StructureSave structure in save.Structures)
                ValidateStructure(structure, players, rosters, siteFacilities, squadrons, adoptedSquadrons);
            foreach (UnitSave unit in save.Units)
                ValidateUnit(unit, players, rosters, shipsData, squadrons, adoptedSquadrons);
            ValidatePlayers(save.Players, players, rosters, levels, squadrons);
            ValidateCombatAndVictory(save, players, entityIds);
        }

        private void ValidateMapCounts(BattleSave save, BattleDefinition definition)
        {
            if (_mapSettings == null) return;
            MapSizeSettings size = _mapSettings.GetSize(definition.Map.Size);
            int expectedZones = definition.Map.Type == MapType.Full ? size.CapturableZoneCount : 0;
            if (save.ReinforcementZones.Length != expectedZones)
                throw new InvalidDataException($"Saved {save.ReinforcementZones.Length} reinforcement zones; the map has {expectedZones}.");
            int expectedSites = !definition.HasFeature(BattleFeatures.CaptureSites) ? 0
                : definition.Map.Type == MapType.Survival
                    ? (definition.HasFeature(BattleFeatures.MiningSites) ? size.MiningSiteCount : 0)
                    : (definition.HasFeature(BattleFeatures.MiningSites)
                        ? definition.Slots.Count + ((size.MiningSiteCount - 2) / 2) * 2
                        : 0) + (size.BattleSiteCount / 2) * 2;
            if (save.CaptureSites.Length != expectedSites)
                throw new InvalidDataException($"Saved {save.CaptureSites.Length} capture sites; the map has {expectedSites}.");
        }

        private static void ValidateSurvival(BattleSave save, BattleDefinition definition)
        {
            if (definition.Mode != GameMode.Survival)
            {
                if (save.Survival != null) throw new InvalidDataException("Non-Survival save contains Survival wave data.");
                return;
            }
            SurvivalSave survival = save.Survival;
            if (survival == null || survival.WaveIndex < 1 || survival.DirectorWave < 0 ||
                !Enum.IsDefined(typeof(SurvivalPhase), survival.Phase))
                throw new InvalidDataException("Saved Survival wave state is invalid.");
            foreach (UnitTypeSave pending in survival.Pending)
            {
                UnitTypeId type = pending.UnitType;
                if (type.IsShip
                    ? !Enum.IsDefined(typeof(ShipType), type.ShipType) || type.ShipType == ShipType.ISDIII
                    : !Enum.IsDefined(typeof(SquadronType), type.SquadronType))
                    throw new InvalidDataException($"Saved pending wave unit {type} is not valid.");
            }
        }

        private static HashSet<long> ValidateSitesAndZones(BattleSave save,
            IReadOnlyDictionary<PlayerId, BattlePlayer> players, HashSet<long> facilityIds)
        {
            foreach (ReinforcementZoneSave zone in save.ReinforcementZones)
                if ((!zone.Owner.IsNone && !players.ContainsKey(zone.Owner)) ||
                    (!zone.CapturingPlayer.IsNone && !players.ContainsKey(zone.CapturingPlayer)))
                    throw new InvalidDataException("Saved reinforcement zone references an unknown player.");
            var siteFacilities = new HashSet<long>();
            foreach (CaptureSiteSave site in save.CaptureSites)
            {
                if (!Enum.IsDefined(typeof(CaptureSiteState), site.State) ||
                    !Enum.IsDefined(typeof(SiteFacilityType), site.FacilityType) ||
                    (!site.Owner.IsNone && !players.ContainsKey(site.Owner)) ||
                    (!site.CapturingPlayer.IsNone && !players.ContainsKey(site.CapturingPlayer)))
                    throw new InvalidDataException("Saved capture site state is invalid.");
                if (site.State == CaptureSiteState.Operational &&
                    (!facilityIds.Contains(site.FacilityId) || !siteFacilities.Add(site.FacilityId)))
                    throw new InvalidDataException($"Capture site facility {site.FacilityId} is missing from restored structures.");
            }
            return siteFacilities;
        }

        private void ValidateStructure(StructureSave structure, IReadOnlyDictionary<PlayerId, BattlePlayer> players,
            IReadOnlyDictionary<PlayerId, FactionRoster> rosters, HashSet<long> siteFacilities,
            IReadOnlyDictionary<long, UnitSave> squadrons, HashSet<long> adoptedSquadrons)
        {
            if (!players.ContainsKey(structure.Owner))
                throw new InvalidDataException($"Structure {structure.Id} belongs to unknown player {structure.Owner}.");
            switch (structure.Kind)
            {
                case StructureKind.SpaceStation:
                    if (!Enum.IsDefined(typeof(FactionType), structure.StationFaction))
                        throw new InvalidDataException($"Station {structure.Id} has unknown faction {structure.StationFaction}.");
                    ValidateHardPoints(_assets.LoadComponent<SpaceStationEntity>(structure.StationFaction + "SpaceStationView"),
                        structure.Health, structure.Id);
                    ValidateHangar(structure.Hangar, 1, squadrons, adoptedSquadrons, structure.Id);
                    break;
                case StructureKind.MiningFacility:
                    MiningFacilityType mineType = structure.MiningFacilityType;
                    if (!Enum.IsDefined(typeof(MiningFacilityType), mineType) ||
                        (!siteFacilities.Contains(structure.Id) && rosters.TryGetValue(structure.Owner, out FactionRoster mineRoster) &&
                         !mineRoster.MiningFacilities.ContainsKey(mineType)))
                        throw new InvalidDataException($"Mining facility {mineType} cannot be restored for {structure.Owner}.");
                    ValidateHardPoints(_assets.LoadComponent<MiningFacilityEntity>(
                        (mineType == MiningFacilityType.AsteroidMiner ? "Asteroid" : "") + "MiningFacilityView"),
                        structure.Health, structure.Id);
                    ValidateHangar(structure.Hangar, 0, squadrons, adoptedSquadrons, structure.Id);
                    break;
                case StructureKind.DefendPlatform:
                    DefendPlatformType platformType = structure.DefendPlatformType;
                    if (!Enum.IsDefined(typeof(DefendPlatformType), platformType) ||
                        (!siteFacilities.Contains(structure.Id) && rosters.TryGetValue(structure.Owner, out FactionRoster platRoster) &&
                         !platRoster.DefendPlatforms.ContainsKey(platformType)))
                        throw new InvalidDataException($"Defense platform {platformType} cannot be restored for {structure.Owner}.");
                    string prefab = (platformType == DefendPlatformType.BattleAsteroid ? "Asteroid" : "") + (platformType switch
                    {
                        DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformView",
                        DefendPlatformType.Empress => "AotrEmpressDefensePlatformView",
                        _ => "DefendPlatformView"
                    });
                    ValidateHardPoints(_assets.LoadComponent<DefendPlatformEntity>(prefab), structure.Health, structure.Id);
                    ValidateHangar(structure.Hangar, 0, squadrons, adoptedSquadrons, structure.Id);
                    break;
                default:
                    throw new InvalidDataException($"Unknown structure kind {structure.Kind}.");
            }
        }

        private void ValidateUnit(UnitSave unit, IReadOnlyDictionary<PlayerId, BattlePlayer> players,
            IReadOnlyDictionary<PlayerId, FactionRoster> rosters, ShipsData shipsData,
            IReadOnlyDictionary<long, UnitSave> squadrons, HashSet<long> adoptedSquadrons)
        {
            if (!players.ContainsKey(unit.Owner))
                throw new InvalidDataException($"Unit {unit.Id} belongs to unknown player {unit.Owner}.");
            if (unit.UnitType.IsShip)
            {
                ShipType shipType = unit.UnitType.ShipType;
                if (!Enum.IsDefined(typeof(ShipType), shipType) || shipType == ShipType.ISDIII ||
                    (rosters.TryGetValue(unit.Owner, out FactionRoster roster) && !roster.Ships.ContainsKey(shipType)))
                    throw new InvalidDataException($"Ship {shipType} cannot be restored for {unit.Owner}.");
                ShipData data = _assets.Load<ShipData>(shipsData.GetShipDataPath(shipType));
                ValidateHardPoints(_assets.LoadComponent<ShipEntity>(shipType + "ShipView"), unit.Health, unit.Id);
                ValidateAbilities(unit.Abilities, data.Abilities, unit.Id);
                ValidateHangar(unit.Hangar, data.HangarBays.Count, squadrons, adoptedSquadrons, unit.Id);
                return;
            }
            SquadronType squadronType = unit.UnitType.SquadronType;
            if (!Enum.IsDefined(typeof(SquadronType), squadronType))
                throw new InvalidDataException($"Squadron {unit.Id} has unknown squadron type {squadronType}.");
            ValidateAbilities(unit.Abilities, Array.Empty<ShipAbilityId>(), unit.Id);
            ValidateHangar(unit.Hangar, 0, squadrons, adoptedSquadrons, unit.Id);
            int fighters = _assets.LoadComponent<Squadron>(squadronType + "SquadronView").Bindings.SquadronHealth.FighterCount;
            if (unit.Health == null || unit.Health.MemberHull.Length != fighters || unit.Health.MemberShields.Length != fighters)
                throw new InvalidDataException($"Saved {unit.Health?.MemberHull.Length ?? 0} fighters; {squadronType} has {fighters}.");
        }

        private static void ValidateHardPoints(EntityView view, UnitHealthSave health, long entityId)
        {
            int expected = view.Bindings.Health[0].ShipUnits.Count;
            int actual = health != null && health.HardPoints != null ? health.HardPoints.Length : -1;
            if (actual != expected)
                throw new InvalidDataException($"Saved {actual} hardpoints for entity {entityId}; current prefab has {expected}.");
        }

        private static void ValidateAbilities(AbilitySlotSave[] saved, IReadOnlyList<ShipAbilityId> expected, long entityId)
        {
            int count = saved != null ? saved.Length : 0;
            if (count != expected.Count)
                throw new InvalidDataException($"Saved {count} ability slots for unit {entityId}; current data has {expected.Count}.");
            for (int i = 0; i < count; i++)
                if (saved[i].Id != expected[i] || !Enum.IsDefined(typeof(ShipAbilityState), saved[i].State))
                    throw new InvalidDataException($"Saved ability slot {i} ({saved[i].Id}) does not match {expected[i]}.");
        }

        private static void ValidateHangar(HangarSave hangar, int bayCount,
            IReadOnlyDictionary<long, UnitSave> squadrons, HashSet<long> adoptedSquadrons, long entityId)
        {
            if (bayCount == 0)
            {
                if (hangar != null) throw new InvalidDataException($"Entity {entityId} has no hangar, but hangar data was saved.");
                return;
            }
            if (hangar == null || hangar.Reserve == null || hangar.Reserve.Length != bayCount)
                throw new InvalidDataException(
                    $"Saved {hangar?.Reserve?.Length ?? 0} hangar bays for entity {entityId}; current data has {bayCount}.");
            int[] remaining = (int[])hangar.Reserve.Clone();
            foreach (LaunchedSquadronSave launched in hangar.Launched)
            {
                if (launched.Bay < 0 || launched.Bay >= bayCount || remaining[launched.Bay]-- <= 0)
                    throw new InvalidDataException($"Invalid hangar bay {launched.Bay} on entity {entityId}.");
                if (!squadrons.ContainsKey(launched.SquadronId) || !adoptedSquadrons.Add(launched.SquadronId))
                    throw new InvalidDataException($"Launched squadron {launched.SquadronId} is missing from restored units.");
            }
        }

        private static void ValidatePlayers(PlayerSave[] saves, IReadOnlyDictionary<PlayerId, BattlePlayer> players,
            IReadOnlyDictionary<PlayerId, FactionRoster> rosters, StationLevelData levels,
            IReadOnlyDictionary<long, UnitSave> squadrons)
        {
            var seen = new HashSet<PlayerId>();
            foreach (PlayerSave save in saves)
            {
                if (!seen.Add(save.Player) || !rosters.TryGetValue(save.Player, out FactionRoster roster) ||
                    save.StationLevel < 1 || save.StationLevel > levels.MaxLevel)
                    throw new InvalidDataException($"Player {save.Player} state is invalid.");
                foreach (ResearchSave research in save.Research)
                    if (!Enum.IsDefined(typeof(ResearchType), research.Type) ||
                        !roster.ResearchLines.TryGetValue(research.Type, out ResearchLineData line) ||
                        research.CompletedTiers < 0 || research.CompletedTiers > line.Tiers.Length)
                        throw new InvalidDataException($"Saved research {research.Type} is not valid for {save.Player}.");
                ValidateRequests(save.Production, roster, levels);
                ValidateRequests(save.Reserve, roster, levels);
                ValidateRequests(save.PendingBuilds, roster, levels);
                foreach (long id in save.PaidSquadrons)
                    if (!squadrons.TryGetValue(id, out UnitSave unit) || unit.Owner != save.Player ||
                        !roster.Squadrons.ContainsKey(unit.UnitType.SquadronType))
                        throw new InvalidDataException($"Paid squadron {id} is missing from restored units.");
                bool isAi = players[save.Player].Slot.IsAi;
                foreach (SuperWeaponSave weapon in save.SuperWeapons)
                    if (!Enum.IsDefined(typeof(SuperWeaponType), weapon.Type) ||
                        !Enum.IsDefined(typeof(SuperWeaponState), weapon.State) ||
                        (isAi && weapon.State == SuperWeaponState.Charging && !roster.SuperWeapons.ContainsKey(weapon.Type)))
                        throw new InvalidDataException($"Saved superweapon {weapon.Type} is not valid for {save.Player}.");
            }
        }

        private static void ValidateRequests(UnitRequestSave[] requests, FactionRoster roster, StationLevelData levels)
        {
            foreach (UnitRequestSave request in requests)
            {
                bool valid = request.Kind switch
                {
                    UnitRequestKind.Ship => Enum.IsDefined(typeof(ShipType), (ShipType)request.Key) &&
                        roster.Ships.ContainsKey((ShipType)request.Key),
                    UnitRequestKind.Squadron => Enum.IsDefined(typeof(SquadronType), (SquadronType)request.Key) &&
                        roster.Squadrons.ContainsKey((SquadronType)request.Key),
                    UnitRequestKind.Level => request.Key >= 1 && levels.GetLevelFactionData(request.Key) != null,
                    UnitRequestKind.MiningFacility => Enum.IsDefined(typeof(MiningFacilityType), (MiningFacilityType)request.Key) &&
                        roster.MiningFacilities.ContainsKey((MiningFacilityType)request.Key),
                    UnitRequestKind.DefendPlatform => Enum.IsDefined(typeof(DefendPlatformType), (DefendPlatformType)request.Key) &&
                        roster.DefendPlatforms.ContainsKey((DefendPlatformType)request.Key),
                    UnitRequestKind.Research => Enum.IsDefined(typeof(ResearchType), (ResearchType)request.Key) &&
                        roster.ResearchLines.TryGetValue((ResearchType)request.Key, out ResearchLineData line) &&
                        request.Tier >= 1 && request.Tier <= line.Tiers.Length,
                    UnitRequestKind.SuperWeapon => Enum.IsDefined(typeof(SuperWeaponType), (SuperWeaponType)request.Key) &&
                        roster.SuperWeapons.ContainsKey((SuperWeaponType)request.Key),
                    _ => false
                };
                if (!valid) throw new InvalidDataException($"Saved production request ({request.Kind}, {request.Key}) is not valid.");
            }
        }

        private static void ValidateCombatAndVictory(BattleSave save,
            IReadOnlyDictionary<PlayerId, BattlePlayer> players, HashSet<long> entityIds)
        {
            foreach (int baseOwner in save.ObservedBases)
                if (!players.ContainsKey(new PlayerId(baseOwner)))
                    throw new InvalidDataException($"Observed base owner {baseOwner} is not in the battle.");
            if (save.SuperWeaponSalvos != null)
                foreach (SuperWeaponSalvoSave salvo in save.SuperWeaponSalvos)
                    if (!Enum.IsDefined(typeof(SuperWeaponType), salvo.Type) || !entityIds.Contains(salvo.TargetId))
                        throw new InvalidDataException($"Superweapon salvo target {salvo.TargetId} is missing from restored units.");
            if (save.SuperWeaponStuns != null)
                foreach (SuperWeaponStunSave stun in save.SuperWeaponStuns)
                    if (!entityIds.Contains(stun.TargetId))
                        throw new InvalidDataException($"Superweapon stun target {stun.TargetId} is missing from restored units.");
        }
    }
}
