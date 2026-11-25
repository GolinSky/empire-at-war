using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using UnityEngine;

namespace EmpireAtWar.Entities.EndGameUi
{
    public sealed class EndGameLossCatalog
    {
        private readonly FactionCatalog _factions;
        private readonly IShipIconProvider _icons;
        private readonly StationLevelData _stations;
        private readonly DefendPlatformCatalog _platforms;
        private readonly MiningFacilityCatalog _facilities;
        private readonly CaptureSiteData _sites;

        public EndGameLossCatalog(FactionCatalog factions, IShipIconProvider icons, StationLevelData stations,
            DefendPlatformCatalog platforms, MiningFacilityCatalog facilities, CaptureSiteData sites)
        {
            _sites = sites;
            _factions = factions;
            _icons = icons;
            _stations = stations;
            _platforms = platforms;
            _facilities = facilities;
        }

        public IReadOnlyList<(string Name, Sprite Icon, int Count)> Describe(IReadOnlyList<BattleUnitLoss> losses) =>
            losses.Select(Describe).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ToArray();

        private (string Name, Sprite Icon, int Count) Describe(BattleUnitLoss loss)
        {
            switch (loss.Kind)
            {
                case BattleLossKind.Ship:
                    var ship = loss.UnitType.ShipType;
                    // A save can hold losses of a ship since retired from every roster; its icon stays.
                    return _factions.TryGetShipFactionData(ship, out FactionData shipData)
                        ? Describe(shipData, _icons.GetShipIcon(ship), loss.Count)
                        : (RetiredUnitName(ship.ToString()), _icons.GetShipIcon(ship), loss.Count);
                case BattleLossKind.Squadron:
                    var squadron = loss.UnitType.SquadronType;
                    // The legacy Delta-7 has an icon but no faction roster entry.
                    if (squadron == SquadronType.Delta7)
                        return ("Delta-7", _icons.GetSquadronIcon(squadron), loss.Count);
                    return _factions.TryGetSquadronFactionData(squadron, out FactionData squadronData)
                        ? Describe(squadronData, _icons.GetSquadronIcon(squadron), loss.Count)
                        : (RetiredUnitName(squadron.ToString()), _icons.GetSquadronIcon(squadron), loss.Count);
                case BattleLossKind.SpaceStation:
                    return ($"{loss.StationFaction.GetDisplayName()} Space Station (Level {loss.StationLevel})",
                        _stations.GetSecondStationFactionData(loss.StationFaction).Icon, loss.Count);
                case BattleLossKind.DefendPlatform:
                    if (loss.DefendPlatformType == DefendPlatformType.BattleAsteroid)
                        return Describe(_sites.GetCost(SiteFacilityType.BattleAsteroid), loss.Count);
                    var platform = _platforms.Get(loss.DefendPlatformType);
                    return Describe(platform, platform.Icon, loss.Count);
                case BattleLossKind.MiningFacility:
                    // Site-built miners have no purchase entry; the capture-site data names them.
                    if (loss.MiningFacilityType == MiningFacilityType.AsteroidMiner)
                        return Describe(_sites.GetCost(SiteFacilityType.Mining), loss.Count);
                    // A battle miner is the common miner placed on a battle map.
                    var facility = _facilities.Get(loss.MiningFacilityType == MiningFacilityType.BattleMiner
                        ? MiningFacilityType.CommonMiner
                        : loss.MiningFacilityType);
                    return Describe(facility, facility.Icon, loss.Count);
                default:
                    throw new ArgumentOutOfRangeException(nameof(loss));
            }
        }

        /// <summary>"CorellianBattlecruiser" → "Corellian Battlecruiser".</summary>
        private static string RetiredUnitName(string typeName) =>
            Regex.Replace(typeName, "(?<=[a-z0-9])(?=[A-Z])", " ");

        private static (string Name, Sprite Icon, int Count) Describe(SiteFacilityCost cost, int count) =>
            (cost.Name, cost.Icon, count);

        private static (string Name, Sprite Icon, int Count) Describe(FactionData data, Sprite icon, int count) =>
            (string.IsNullOrWhiteSpace(data.DisplayName) ? data.Name : data.DisplayName, icon, count);
    }
}
