using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ControlGroups;
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
        private const int LEGACY_CD273_HARDPOINT_COUNT = 10;
        private const int CD273_HARDPOINT_COUNT = 16;
        private const int LEGACY_DH_OMNI_HARDPOINT_COUNT = 4;
        private const int DH_OMNI_HARDPOINT_COUNT = 14;
        private const int LEGACY_INTERDICTOR_HARDPOINT_COUNT = 23;
        private const int INTERDICTOR_HARDPOINT_COUNT = 24;
        private const int LEGACY_INVINCIBLE_HARDPOINT_COUNT = 30;
        private const int INVINCIBLE_HARDPOINT_COUNT = 32;
        private const int LEGACY_SECUTOR_HARDPOINT_COUNT = 46;
        private const int SECUTOR_HARDPOINT_COUNT = 47;

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
            ValidateControlGroups(save.ControlGroups);
            ValidateLosses(save.PlayerLosses);
            ValidateLosses(save.EnemyLosses);

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
                if (structure.Kind is not (StructureKind.SpaceStation or StructureKind.SecondStation))
                    facilityIds.Add(structure.Id);
            }
            foreach (UnitSave unit in save.Units)
            {
                if (!entityIds.Add(unit.Id)) throw new InvalidDataException($"Duplicate entity id {unit.Id}.");
                if (!unit.UnitType.IsShip) squadrons.Add(unit.Id, unit);
            }

            var siteFacilities = ValidateSitesAndZones(save, players, facilityIds);
            var adoptedSquadrons = new HashSet<long>();
            List<(UnitSave Unit, HangarSave Hangar)> migratedHangars =
                save.Header.Version == BattleSaveHeader.PRE_HANGAR_REBALANCE_VERSION ? new() : null;
            var migratedHealth = new List<(UnitSave Unit, UnitHealthSave Health)>();
            var migratedAbilities = new List<(UnitSave Unit, AbilitySlotSave[] Slots)>();
            foreach (StructureSave structure in save.Structures)
                ValidateStructure(structure, players, rosters, siteFacilities, squadrons, adoptedSquadrons);
            foreach (UnitSave unit in save.Units)
                ValidateUnit(unit, players, rosters, shipsData, squadrons, adoptedSquadrons, migratedHangars,
                    migratedHealth, migratedAbilities);
            ValidatePlayers(save.Players, players, rosters, levels, squadrons);
            ValidateCombatAndVictory(save, players, entityIds);

            foreach ((UnitSave unit, UnitHealthSave health) in migratedHealth)
                unit.SetMigratedHealth(health);
            foreach ((UnitSave unit, AbilitySlotSave[] slots) in migratedAbilities)
                unit.WithAbilities(slots);

            if (migratedHangars != null)
            {
                // Commit only after all checks pass, so a rejected or retried load keeps its original reserves.
                foreach ((UnitSave unit, HangarSave hangar) in migratedHangars)
                    unit.WithHangar(hangar);
                save.Header.FinishHangarMigration();
            }
        }

        public static void ValidateControlGroups(ControlGroupSave[] groups)
        {
            var slots = new HashSet<(PlayerId Owner, int Slot)>();
            foreach (ControlGroupSave group in groups)
            {
                if (group.Slot < 0 || group.Slot >= ControlGroupModel.SLOT_COUNT)
                    throw new InvalidDataException($"Saved squad slot {group.Slot} is outside 0–9.");
                if (!slots.Add((group.Owner, group.Slot)))
                    throw new InvalidDataException($"Duplicate saved squad slot {group.Slot} for player {group.Owner}.");
            }
        }

        private static void ValidateLosses(IReadOnlyList<BattleUnitLoss> losses)
        {
            var types = new HashSet<(BattleLossKind, int, int)>();
            foreach (BattleUnitLoss loss in losses)
            {
                if (loss == null || loss.Count <= 0)
                    throw new InvalidDataException("Saved battle losses must have positive counts.");
                bool validType = loss.Kind switch
                {
                    BattleLossKind.Ship => Enum.IsDefined(typeof(ShipType), loss.UnitType.ShipType),
                    BattleLossKind.Squadron => Enum.IsDefined(typeof(SquadronType), loss.UnitType.SquadronType),
                    BattleLossKind.SpaceStation => Enum.IsDefined(typeof(FactionType), loss.StationFaction),
                    BattleLossKind.DefendPlatform => Enum.IsDefined(typeof(DefendPlatformType), loss.DefendPlatformType),
                    BattleLossKind.MiningFacility => Enum.IsDefined(typeof(MiningFacilityType), loss.MiningFacilityType),
                    _ => false
                };
                bool validLevel = loss.Kind == BattleLossKind.SpaceStation ? loss.StationLevel > 0 : loss.StationLevel == 0;
                if (!validType || !validLevel || !types.Add((loss.Kind, loss.TypeId, loss.StationLevel)))
                    throw new InvalidDataException("Saved battle losses contain an invalid or duplicate type.");
            }
        }

        private void ValidateMapCounts(BattleSave save, BattleDefinition definition)
        {
            if (_mapSettings == null) return;
            MapSizeSettings size = _mapSettings.GetSize(definition.Map.Size);
            if (definition.Map.Type == MapType.Full)
            {
                PlanetMapProfileEntry profile = _mapSettings.Profiles.Get(definition.Map.Planet);
                if (profile.Archetype.Kind != MapArchetypeKind.OpenField || definition.Slots.Count > 4)
                    size = size.ForArchetype(profile, definition.Slots.Count);
            }
            int expectedZones = definition.Map.Type == MapType.Full ? 1 + 2 * size.AdditionalZonePairCount : 0;
            if (save.ReinforcementZones.Length != expectedZones)
                throw new InvalidDataException($"Saved {save.ReinforcementZones.Length} reinforcement zones; the map has {expectedZones}.");
            int expectedSites = 0;
            if (definition.HasFeature(BattleFeatures.CaptureSites) && definition.HasFeature(BattleFeatures.MiningSites))
            {
                if (definition.Map.Type == MapType.Survival)
                {
                    if (save.CaptureSites.Length == SurvivalLayoutGenerator.GetSiteCount(size, true)) return;
                    expectedSites = SurvivalLayoutGenerator.GetSiteCount(size, false);
                }
                else if (definition.Map.Type == MapType.Full)
                    expectedSites = definition.Slots.Count * (1 + _mapSettings.HomeSatelliteCount) +
                        _mapSettings.MainZoneMiningCount + _mapSettings.MainZoneSatelliteCount +
                        2 * size.AdditionalZonePairCount * (1 + _mapSettings.AdditionalZoneSatelliteCount) +
                        size.FreeSatelliteCount;
            }
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
                    ? !Enum.IsDefined(typeof(ShipType), type.ShipType)
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
                if ((site.State == CaptureSiteState.Operational || site.State == CaptureSiteState.Constructing) &&
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
            if (structure.Kind is not (StructureKind.SpaceStation or StructureKind.SecondStation) &&
                (!float.IsFinite(structure.ConstructionProgress) || structure.ConstructionProgress < 0f ||
                 structure.ConstructionProgress > 1f || !float.IsFinite(structure.ConstructionBuildTime) ||
                 structure.ConstructionBuildTime < 0f ||
                 (structure.ConstructionProgress < 1f && structure.ConstructionBuildTime == 0f)))
                throw new InvalidDataException($"Structure {structure.Id} has invalid construction progress or duration.");
            switch (structure.Kind)
            {
                case StructureKind.SpaceStation:
                case StructureKind.SecondStation:
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
                        MiningFacilityAssets.GetName(mineType) + "View"),
                        structure.Health, structure.Id);
                    ValidateHangar(structure.Hangar, 0, squadrons, adoptedSquadrons, structure.Id);
                    break;
                case StructureKind.DefendPlatform:
                    DefendPlatformType platformType = structure.DefendPlatformType;
                    if (!Enum.IsDefined(typeof(DefendPlatformType), platformType) ||
                        (!siteFacilities.Contains(structure.Id) && rosters.TryGetValue(structure.Owner, out FactionRoster platRoster) &&
                         !platRoster.DefendPlatforms.ContainsKey(platformType)))
                        throw new InvalidDataException($"Defense platform {platformType} cannot be restored for {structure.Owner}.");
                    ValidateHardPoints(_assets.LoadComponent<DefendPlatformEntity>(
                        DefendPlatformAssets.GetViewName(platformType)), structure.Health, structure.Id);
                    ValidateHangar(structure.Hangar, 0, squadrons, adoptedSquadrons, structure.Id);
                    break;
                default:
                    throw new InvalidDataException($"Unknown structure kind {structure.Kind}.");
            }
        }

        private void ValidateUnit(UnitSave unit, IReadOnlyDictionary<PlayerId, BattlePlayer> players,
            IReadOnlyDictionary<PlayerId, FactionRoster> rosters, ShipsData shipsData,
            IReadOnlyDictionary<long, UnitSave> squadrons, HashSet<long> adoptedSquadrons,
            List<(UnitSave Unit, HangarSave Hangar)> migratedHangars,
            List<(UnitSave Unit, UnitHealthSave Health)> migratedHealth,
            List<(UnitSave Unit, AbilitySlotSave[] Slots)> migratedAbilities)
        {
            if (!players.ContainsKey(unit.Owner))
                throw new InvalidDataException($"Unit {unit.Id} belongs to unknown player {unit.Owner}.");
            if (unit.UnitType.IsShip)
            {
                ShipType shipType = unit.UnitType.ShipType;
                if (!Enum.IsDefined(typeof(ShipType), shipType) ||
                    (rosters.TryGetValue(unit.Owner, out FactionRoster roster) && !UnitRequestSave.CanRestoreShip(roster, shipType)))
                    throw new InvalidDataException($"Ship {shipType} cannot be restored for {unit.Owner}.");
                ShipData data = _assets.Load<ShipData>(shipsData.GetShipDataPath(shipType));
                ShipEntity view = _assets.LoadComponent<ShipEntity>(shipType + "ShipView");
                UnitHealthSave health = unit.Health;
                (int legacyCount, int currentCount) = shipType switch
                {
                    ShipType.CD273 => (LEGACY_CD273_HARDPOINT_COUNT, CD273_HARDPOINT_COUNT),
                    ShipType.DHOmni => (LEGACY_DH_OMNI_HARDPOINT_COUNT, DH_OMNI_HARDPOINT_COUNT),
                    ShipType.InterdictorStarDestroyer => (LEGACY_INTERDICTOR_HARDPOINT_COUNT, INTERDICTOR_HARDPOINT_COUNT),
                    ShipType.Invincible => (LEGACY_INVINCIBLE_HARDPOINT_COUNT, INVINCIBLE_HARDPOINT_COUNT),
                    ShipType.Secutor => (LEGACY_SECUTOR_HARDPOINT_COUNT, SECUTOR_HARDPOINT_COUNT),
                    _ => (0, 0)
                };
                if (legacyCount > 0 && health?.HardPoints?.Length == legacyCount &&
                    view.Bindings.Health[0].ShipUnits.Count == currentCount)
                {
                    // Newly appended hardpoints start at full health.
                    var hardPoints = new float[currentCount];
                    Array.Copy(health.HardPoints, hardPoints, legacyCount);
                    Array.Fill(hardPoints, 1f, legacyCount, currentCount - legacyCount);
                    health = UnitHealthSave.ForShip(health.Hull, health.Shields, hardPoints);
                    migratedHealth.Add((unit, health));
                }
                ValidateHardPoints(view, health, unit.Id);
                AbilitySlotSave[] slots = MigrateShipAbilities(shipType, unit.Abilities);
                ValidateAbilities(slots, data.Abilities, unit.Id);
                if (slots != unit.Abilities)
                    migratedAbilities.Add((unit, slots));
                HangarSave hangar = unit.Hangar;
                if (migratedHangars != null && HangarStockMigration.TryMigrate(unit, data.HangarBays, out HangarSave migrated))
                {
                    hangar = migrated;
                    migratedHangars.Add((unit, migrated));
                }
                ValidateHangar(hangar, data.HangarBays.Count, squadrons, adoptedSquadrons, unit.Id);
                return;
            }
            SquadronType squadronType = unit.UnitType.SquadronType;
            if (!Enum.IsDefined(typeof(SquadronType), squadronType))
                throw new InvalidDataException($"Squadron {unit.Id} has unknown squadron type {squadronType}.");
            SquadronData squadronData = _assets.Load<SquadronData>(squadronType + "SquadronData");
            ValidateAbilities(unit.Abilities, squadronData.Abilities, unit.Id);
            ValidateHangar(unit.Hangar, 0, squadrons, adoptedSquadrons, unit.Id);
            int fighters = _assets.LoadComponent<Squadron>(squadronType + "SquadronView").Bindings.SquadronHealth.FighterCount;
            if (unit.Health == null || unit.Health.MemberHull.Length != fighters || unit.Health.MemberShields.Length != fighters)
                throw new InvalidDataException($"Saved {unit.Health?.MemberHull.Length ?? 0} fighters; {squadronType} has {fighters}.");
        }

        private static AbilitySlotSave[] MigrateShipAbilities(ShipType shipType, AbilitySlotSave[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                if (shipType == ShipType.Secutor)
                    return new[]
                    {
                        AbilitySlotSave.Ready(ShipAbilityId.BoostShieldPower),
                        AbilitySlotSave.Ready(ShipAbilityId.BoostWeaponPower)
                    };
                return slots;
            }
            return shipType switch
            {
                ShipType.QuasarFire when slots.Length == 2 && slots[0] is { Id: ShipAbilityId.BoostWeaponPower } &&
                    slots[1] is { Id: ShipAbilityId.TractorBeam } => new[] { slots[0] },
                ShipType.BothanAssaultCruiser when slots.Length == 2 && slots[0] is { Id: ShipAbilityId.BothanCoordinatedAssault } &&
                    slots[1] is { Id: ShipAbilityId.TractorBeam } => new[] { slots[0] },
                ShipType.InterdictorStarDestroyer when slots.Length == 1 && slots[0] is { Id: ShipAbilityId.BoostWeaponPower }
                    => new[] { slots[0].WithId(ShipAbilityId.DauntlessSensorJamming), AbilitySlotSave.Ready(ShipAbilityId.Interdict) },
                ShipType.InterdictorStarDestroyer when slots.Length == 2 && slots[0] is { Id: ShipAbilityId.BoostWeaponPower } &&
                    slots[1] is { Id: ShipAbilityId.DauntlessSensorJamming }
                    => new[] { slots[1], AbilitySlotSave.Ready(ShipAbilityId.Interdict) },
                ShipType.Praetor when slots.Length == 1 && slots[0] is { Id: ShipAbilityId.FullSalvo }
                    => new[] { slots[0], AbilitySlotSave.Ready(ShipAbilityId.BoostShieldPower) },
                ShipType.Praetor when slots.Length == 3 && slots[0] is { Id: ShipAbilityId.FullSalvo } &&
                    slots[1] is { Id: ShipAbilityId.BoostShieldPower } && slots[2] is { Id: ShipAbilityId.BoostWeaponPower }
                    => new[] { slots[0], slots[1] },
                ShipType.MC80Justice when slots.Length == 1 && slots[0] is { Id: ShipAbilityId.BoostShieldPower }
                    => new[] { slots[0].WithId(ShipAbilityId.BoostWeaponPower) },
                _ => slots
            };
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
            bool hasShieldPower = false;
            for (int i = 0; i < count; i++)
            {
                if (saved[i].Id != ShipAbilityId.BoostShieldPower) continue;
                if (hasShieldPower)
                    throw new InvalidDataException($"Saved unit {entityId} has multiple shield power slots after legacy migration.");
                hasShieldPower = true;
            }
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
            foreach (LaunchedSquadronSave launched in hangar.Launched)
            {
                if (launched.Bay < 0 || launched.Bay >= bayCount)
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
                        UnitRequestSave.CanRestoreShip(roster, (ShipType)request.Key),
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
                    UnitRequestKind.SecondStation => request.Key == 0,
                    UnitRequestKind.SecondStationLevel =>
                        request.Key >= 1 && levels.GetSecondStationLevelFactionData(request.Key) != null,
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
