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
    }
}
