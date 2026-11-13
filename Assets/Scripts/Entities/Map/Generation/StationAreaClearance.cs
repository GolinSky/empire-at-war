using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Home capture points use their station's team even though the points begin neutral.</summary>
    public static class StationAreaClearance
    {
        public static bool HasEnemyClearance(MapNode candidate, MapSizeSettings size,
            IReadOnlyList<MapStation> stations, IReadOnlyList<PlayerSlot> players)
        {
            PlayerId owner = candidate.IsHomeSite ? stations[candidate.PointId].Owner : candidate.Owner;
            TeamId team = GetTeam(owner, players);
            foreach (MapStation station in stations)
            {
                if (GetTeam(station.Owner, players) == team) continue;
                // Match the center-to-center exclusion used for neutral relay placement.
                if (PlanarGeometry.Distance(candidate.Center, station.Position) < size.AdditionalZoneStationDistance)
                    return false;
            }
            return true;
        }

        private static TeamId GetTeam(PlayerId owner, IReadOnlyList<PlayerSlot> players)
        {
            foreach (PlayerSlot player in players)
                if (player.Id == owner) return player.Team;
            throw new InvalidOperationException($"Station area owner {owner} is absent from the roster.");
        }
    }
}
