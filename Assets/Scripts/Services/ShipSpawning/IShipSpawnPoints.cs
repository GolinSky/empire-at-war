using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>Finds ship arrival points that pass the reinforcement spawn rule.</summary>
    public interface IShipSpawnPoints
    {
        bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position);

        /// <summary>Open point around any living owned station, the one nearest the home zone first.</summary>
        bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        /// <summary>
        /// Forward arrival beside own units: per unit, 8 fixed points on a ring around it, the first straight away
        /// from <paramref name="awayFrom"/>, then alternating left and right. A point needs bounds, no hazard, vision,
        /// no hostile block circle and a clear hull; no reachability or threat test (the unit already stands there).
        /// </summary>
        bool TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<Vector3> units,
            Vector3 awayFrom, out Vector3 position, out ForwardSpawnAttempt attempt);

        /// <summary>Planar hull radius, for deploying small ships first.</summary>
        float GetHullRadius(ShipType shipType);

        bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        /// <summary>Finds a safe arrival point behind the defended station.</summary>
        bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);

        void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position);

        void ReleaseDefenseLanding(object token);

        /// <summary>Finds the clear point farthest from the threats within the defended station perimeter.</summary>
        bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);

        /// <summary>Ring search (1, 1.5, 2, 3x HomeAreaRadius) around anchor: rear half-plane, then sides, then front.</summary>
        bool TryGetRearSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, bool requireThreatSafety, out Vector3 position);

        /// <summary>Finds the clear point farthest from threats on rings out to 3x HomeAreaRadius using the station-defense rule.</summary>
        bool TryGetWideDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);
    }
}
