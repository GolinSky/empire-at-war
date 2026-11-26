using System;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public struct ResearchEffect
    {
        [Tooltip("Unit classes the effect applies to. Ignored for Income and StationIncome.")]
        [SerializeField] private ShipClass[] shipClasses;

        [SerializeField] private ResearchStat stat;

        [SerializeField] private float multiplier;

        public ResearchStat Stat => stat;
        public float Multiplier => multiplier;
        public ShipClass[] ShipClasses => shipClasses;
    }
}
