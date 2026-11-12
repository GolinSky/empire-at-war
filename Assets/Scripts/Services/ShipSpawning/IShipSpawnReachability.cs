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
    }
}
