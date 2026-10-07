using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Entities.EnemyFaction.Data
{
    /// <summary>One row of the AI's matchup knowledge: what a ship class hunts well and what it fears.</summary>
    [Serializable]
    public struct ShipClassMatchup
    {
        [SerializeField] private ShipClass shipClass;
        [SerializeField] private ShipClass[] strongAgainst;
        [SerializeField] private ShipClass[] weakAgainst;

        public ShipClass ShipClass => shipClass;
        public ShipClass[] StrongAgainst => strongAgainst;
        public ShipClass[] WeakAgainst => weakAgainst;

        public ShipClassMatchup(ShipClass shipClass, ShipClass[] strongAgainst, ShipClass[] weakAgainst)
        {
            this.shipClass = shipClass;
            this.strongAgainst = strongAgainst;
            this.weakAgainst = weakAgainst;
        }
    }
}
