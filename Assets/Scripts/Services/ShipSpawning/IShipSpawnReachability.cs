using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>Whether a ship arriving at a point could fly from there to its own station's space,
    /// so a free spot walled in by asteroids never traps it.</summary>
    public interface IShipSpawnReachability
    {
        bool IsReachable(PlayerId owner, ShipType shipType, Vector3 position);

        void FillReachability(PlayerId owner, ShipType shipType, Vector3[] positions, bool[] reachable);

        /// <summary>Leaves the published mask unchanged while a new immutable grid snapshot is pending.</summary>
        bool TryFillReachability(PlayerId owner, ShipType shipType, Vector3[] positions, bool[] reachable,
            bool forceRefresh);
    }
}
