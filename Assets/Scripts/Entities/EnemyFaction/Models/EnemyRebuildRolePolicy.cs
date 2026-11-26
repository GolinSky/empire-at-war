using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// A wiped fleet that cannot land or pay for a capital rebuilds from mid-size warships that hurt capitals:
    /// frigates, cruisers and heavy cruisers with an anti-capital, artillery, carrier or damage role. These bypass the
    /// station-level tier floor; the counter model picks among them (user 2026-11-26).
    /// </summary>
    public static class EnemyRebuildRolePolicy
    {
        public const UnitRole REBUILD_ROLES =
            UnitRole.AntiCapital | UnitRole.Artillery | UnitRole.Carrier | UnitRole.Dps;

        public static bool IsRebuildShip(ShipClass shipClass, UnitRole roles) =>
            (shipClass is ShipClass.Frigate or ShipClass.Cruiser or ShipClass.HeavyCruiser) &&
            (roles & REBUILD_ROLES) != 0;

        /// <param name="activeShips">Own combat ships alive.</param>
        /// <param name="escortShips">Ships a stationless capital needs beside it to land.</param>
        /// <param name="hasLandableAffordableCapital">An unlocked assault capital can land now and is paid for.</param>
        public static bool IsRebuilding(int activeShips, int escortShips, bool hasLandableAffordableCapital) =>
            activeShips < escortShips && !hasLandableAffordableCapital;
    }
}
