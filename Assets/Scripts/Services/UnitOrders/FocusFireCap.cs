using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Main-target slots one target class offers each attacking team; 0 = unlimited.</summary>
    [Serializable]
    public sealed class FocusFireCap
    {
        [SerializeField] private ShipClass shipClass;
        [SerializeField, Min(0)] private int shipSlots;
        [SerializeField, Min(0)] private int squadronSlots;

        public ShipClass ShipClass => shipClass;
        public int ShipSlots => shipSlots;
        public int SquadronSlots => squadronSlots;

        public FocusFireCap(ShipClass shipClass, int shipSlots, int squadronSlots)
        {
            this.shipClass = shipClass;
            this.shipSlots = shipSlots;
            this.squadronSlots = squadronSlots;
        }
    }
}
