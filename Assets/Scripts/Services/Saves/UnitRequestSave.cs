using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>A purchase by kind and key; its price and build time come from the faction data again on load.</summary>
    [Serializable]
    public sealed class UnitRequestSave
    {
        [SerializeField] private UnitRequestKind kind;
        [SerializeField] private int key;
        [SerializeField] private int tier;
        [SerializeField] private float timeLeft;

        public UnitRequestKind Kind => kind;
        public int Key => key;
        public int Tier => tier;
        public float TimeLeft => timeLeft;

        public static UnitRequestSave From(UnitRequest request, float timeLeft = 0f)
        {
            UnitRequestSave save = new UnitRequestSave { timeLeft = timeLeft };
            switch (request)
            {
                case ShipUnitRequest ship: save.kind = UnitRequestKind.Ship; save.key = (int)ship.Key; break;
                case SquadronUnitRequest squadron: save.kind = UnitRequestKind.Squadron; save.key = (int)squadron.Key; break;
                case LevelUnitRequest level: save.kind = UnitRequestKind.Level; save.key = level.Key; break;
                case MiningFacilityUnitRequest mining: save.kind = UnitRequestKind.MiningFacility; save.key = (int)mining.Key; break;
                case DefendPlatformUnitRequest platform: save.kind = UnitRequestKind.DefendPlatform; save.key = (int)platform.Key; break;
                case ResearchUnitRequest research:
                    save.kind = UnitRequestKind.Research; save.key = (int)research.Key; save.tier = research.Tier; break;
                case SuperWeaponUnitRequest superWeapon: save.kind = UnitRequestKind.SuperWeapon; save.key = (int)superWeapon.Key; break;
                default: throw new ArgumentOutOfRangeException(nameof(request), request.GetType().Name, "Unknown request kind.");
            }

            return save;
        }

        public UnitRequest ToRequest(IUnitRequestFactory factory, FactionRoster roster, StationLevelData levels) => kind switch
        {
            UnitRequestKind.Ship => factory.ConstructUnitRequest(roster.Ships[(ShipType)key], (ShipType)key),
            UnitRequestKind.Squadron => factory.ConstructUnitRequest(roster.Squadrons[(SquadronType)key], (SquadronType)key),
            UnitRequestKind.Level => factory.ConstructUnitRequest(levels.GetLevelFactionData(key), key),
            UnitRequestKind.MiningFacility => factory.ConstructUnitRequest(
                roster.MiningFacilities[(MiningFacilityType)key], (MiningFacilityType)key),
            UnitRequestKind.DefendPlatform => factory.ConstructUnitRequest(
                roster.DefendPlatforms[(DefendPlatformType)key], (DefendPlatformType)key),
            UnitRequestKind.Research => factory.ConstructUnitRequest(
                roster.ResearchLines[(ResearchType)key].Tiers[tier - 1].FactionData, (ResearchType)key, tier),
            UnitRequestKind.SuperWeapon => factory.ConstructUnitRequest(
                roster.SuperWeapons[(SuperWeaponType)key], (SuperWeaponType)key),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown request kind.")
        };
    }
}
