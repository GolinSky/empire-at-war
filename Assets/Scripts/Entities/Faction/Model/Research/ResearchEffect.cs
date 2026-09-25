using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public struct ResearchEffect
    {
        [SerializeField] private ResearchStat stat;
        [SerializeField] private float multiplier;
        [Tooltip("Unit classes the effect applies to. Ignored for Income.")]
        [SerializeField] private ShipClass[] shipClasses;

        public ResearchStat Stat => stat;
        public float Multiplier => multiplier;
        public ShipClass[] ShipClasses => shipClasses;
    }
}
