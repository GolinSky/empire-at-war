using System;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    /// <summary>A purchase by kind and key; shared by player commands, replication and saves.</summary>
    public readonly struct UnitRequestKey
    {
        public UnitRequestKind Kind { get; }
        public int Key { get; }
        public int Tier { get; }

        public UnitRequestKey(UnitRequestKind kind, int key, int tier)
        {
            Kind = kind;
            Key = key;
            Tier = tier;
        }

        public static UnitRequestKey From(UnitRequest request) => request switch
        {
            ShipUnitRequest ship => new UnitRequestKey(UnitRequestKind.Ship, (int)ship.Key, 0),
            SquadronUnitRequest squadron => new UnitRequestKey(UnitRequestKind.Squadron, (int)squadron.Key, 0),
            LevelUnitRequest level => new UnitRequestKey(UnitRequestKind.Level, level.Key, 0),
            MiningFacilityUnitRequest mining => new UnitRequestKey(UnitRequestKind.MiningFacility, (int)mining.Key, 0),
            DefendPlatformUnitRequest platform => new UnitRequestKey(UnitRequestKind.DefendPlatform, (int)platform.Key, 0),
            ResearchUnitRequest research => new UnitRequestKey(UnitRequestKind.Research, (int)research.Key, research.Tier),
            SuperWeaponUnitRequest superWeapon => new UnitRequestKey(UnitRequestKind.SuperWeapon, (int)superWeapon.Key, 0),
            SecondStationUnitRequest => new UnitRequestKey(UnitRequestKind.SecondStation, 0, 0),
            SecondStationLevelUnitRequest secondLevel =>
                new UnitRequestKey(UnitRequestKind.SecondStationLevel, secondLevel.Level, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.GetType().Name, "Unknown request kind.")
        };

        /// <summary>Rebuilds the purchase; its price and build time come from the faction data again.</summary>
        public UnitRequest ToRequest(IUnitRequestFactory factory, FactionRoster roster, StationLevelData levels) => Kind switch
        {
            UnitRequestKind.Ship => factory.ConstructUnitRequest(roster.Ships[(ShipType)Key], (ShipType)Key),
            UnitRequestKind.Squadron => factory.ConstructUnitRequest(roster.Squadrons[(SquadronType)Key], (SquadronType)Key),
            UnitRequestKind.Level => factory.ConstructUnitRequest(levels.GetLevelFactionData(Key), Key),
            UnitRequestKind.MiningFacility => factory.ConstructUnitRequest(
                roster.MiningFacilities[(MiningFacilityType)Key], (MiningFacilityType)Key),
            UnitRequestKind.DefendPlatform => factory.ConstructUnitRequest(
                roster.DefendPlatforms[(DefendPlatformType)Key], (DefendPlatformType)Key),
            UnitRequestKind.Research => factory.ConstructUnitRequest(
                roster.ResearchLines[(ResearchType)Key].Tiers[Tier - 1].FactionData, (ResearchType)Key, Tier),
            UnitRequestKind.SuperWeapon => factory.ConstructUnitRequest(
                roster.SuperWeapons[(SuperWeaponType)Key], (SuperWeaponType)Key),
            UnitRequestKind.SecondStation => factory.ConstructSecondStationRequest(levels.SecondStation),
            UnitRequestKind.SecondStationLevel => factory.ConstructSecondStationLevelRequest(
                levels.GetSecondStationLevelFactionData(Key), Key),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown request kind.")
        };
    }
}
