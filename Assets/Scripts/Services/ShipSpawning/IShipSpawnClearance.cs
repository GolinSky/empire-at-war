using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>The single rule for whether a ship may arrive at a point: its hull must not touch
    /// any collider or the landing hull of a ship still in hyperspace.</summary>
    public interface IShipSpawnClearance
    {
        /// <summary>Planar distance from the ship origin to its farthest hull corner.</summary>
        float GetPlanarRadius(ShipType shipType);

        /// <summary>Checks the hull with its owner's station facing.</summary>
        bool IsClear(PlayerId owner, ShipType shipType, Vector3 position);

        /// <summary>Checks the complete hull turned to the given heading.</summary>
        bool IsClear(ShipType shipType, Vector3 position, Quaternion rotation);

        void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position);

        void ReserveLanding(object ship, ShipType shipType, Vector3 position, Quaternion rotation);

        void ReleaseLanding(object ship);
    }
}
