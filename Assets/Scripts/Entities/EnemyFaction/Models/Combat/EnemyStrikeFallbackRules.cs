using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Chooses the safe point of a hit-and-run cycle: repairer, own station, main fleet body, held zone. A point is
    /// valid when the way to it is not cut off and no known hostile weapon reaches it.
    /// </summary>
    public static class EnemyStrikeFallbackRules
    {
        /// <summary>Gap between the station hull and a ship parked behind it.</summary>
        public const float STATION_COVER_MARGIN = 100f;

        /// <summary>
        /// Index of the valid candidate with the best <see cref="EnemyStrikeFallbackKind"/>; the earlier one on a tie.
        /// While the group defends its station only the station point counts. -1 when none is valid.
        /// </summary>
        public static int Select(IReadOnlyList<EnemyStrikeFallbackCandidate> candidates, bool defendingStation)
        {
            int best = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                EnemyStrikeFallbackCandidate candidate = candidates[i];
                if (candidate.IsCutOff || candidate.IsInHostileReach ||
                    defendingStation && candidate.Kind != EnemyStrikeFallbackKind.Station) continue;
                if (best < 0 || candidate.Kind < candidates[best].Kind) best = i;
            }
            return best;
        }

        /// <summary>
        /// The point behind <paramref name="station"/> on the far side from <paramref name="threat"/>, one
        /// <see cref="STATION_COVER_MARGIN"/> clear of the station hull.
        /// </summary>
        public static FormationPoint GetStationCoverPoint(FormationPoint station, FormationPoint threat,
            float stationRadius, float shipRadius)
        {
            float x = station.X - threat.X;
            float z = station.Z - threat.Z;
            float length = (float)Math.Sqrt(x * x + z * z);
            if (length <= float.Epsilon)
            {
                x = 1f;
                z = 0f;
                length = 1f;
            }
            float distance = stationRadius + shipRadius + STATION_COVER_MARGIN;
            return new FormationPoint(station.X + x / length * distance, station.Z + z / length * distance);
        }
    }
}
