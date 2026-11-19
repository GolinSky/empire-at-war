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

        /// <summary>Finds a safe arrival point behind the defended station.</summary>
        bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);

        void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position);

        void ReleaseDefenseLanding(object token);

        /// <summary>Finds the clear point farthest from the threats within the defended station perimeter.</summary>
        bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position);
    }
}
