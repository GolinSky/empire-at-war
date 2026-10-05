using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>
    /// The single deployment rule for every team: a point is open when it is inside the map, the team sees it
    /// and no hostile spawn blocker covers it. Ships additionally need a clear landing hull; structures need
    /// clear space and must stay outside relay rings and capture sites.
    /// </summary>
    public interface IReinforcementSpawnRule
    {
        bool IsOpen(PlayerId team, Vector3 position);

        bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position);

        bool CanSpawnStructure(PlayerId owner, Vector3 position);
    }
}
