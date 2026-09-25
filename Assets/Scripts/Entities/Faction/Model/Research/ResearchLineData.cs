using System;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class ResearchLineData
    {
        [SerializeField] private ResearchTierData[] tiers;

        public ResearchTierData[] Tiers => tiers;
    }
}
