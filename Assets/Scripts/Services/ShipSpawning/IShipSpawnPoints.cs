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

        bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        /// <summary>Finds a safe arrival point near the defended station or its fallback rally.</summary>
        bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);

        /// <summary>Best open arrival point near the anchor when none is threat-free: farthest from the threats.</summary>
        bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);
    }
}
