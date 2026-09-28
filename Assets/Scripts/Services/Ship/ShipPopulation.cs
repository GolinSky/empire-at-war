using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Ship
{
    public static class ShipPopulation
    {
        private const float SHIP_CAPTURE_STRENGTH = 1f;

        /// <summary>Adds every ship inside the circle to the capture tally; a ship weighs 1.</summary>
        public static void AddShipStrength(this IShipService shipService, Func<Vector3, bool> contains,
            CaptureTallyBuilder tally)
        {
            foreach (IShipEntity ship in shipService.Ships)
            {
                if (contains(ship.WorldPosition))
                {
                    tally.Add(ship.Owner, SHIP_CAPTURE_STRENGTH);
                }
            }
        }
    }
}
