using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>Finds ship arrival points that pass the reinforcement spawn rule.</summary>
    public interface IShipSpawnPoints
    {
        bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position);

        bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position);

        /// <summary>Finds an arrival point within the home-area radius of an arbitrary anchor, such as a surviving station.</summary>
        bool TryGetSpawnPositionAround(PlayerId owner, ShipType shipType, Vector3 anchor, out Vector3 position);
    }
}
