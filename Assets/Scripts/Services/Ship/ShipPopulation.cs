using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Ship
{
    public static class ShipPopulation
    {
        public static void CountShips(this IShipService shipService, Func<Vector3, bool> contains,
            out int playerShips, out int opponentShips)
        {
            playerShips = 0;
            opponentShips = 0;
            foreach (IShipEntity ship in shipService.Ships)
            {
                if (!contains(ship.WorldPosition))
                {
                    continue;
                }

                if (ship.PlayerType == PlayerType.Player)
                {
                    playerShips++;
                }
                else if (ship.PlayerType == PlayerType.Opponent)
                {
                    opponentShips++;
                }
            }
        }
    }
}
