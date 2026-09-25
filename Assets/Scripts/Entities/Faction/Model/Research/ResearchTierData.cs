using System;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class ResearchTierData
    {
        [SerializeField] private FactionData factionData;
        [Tooltip("Full effect set of the research line once this tier is complete; it replaces the previous tier.")]
        [SerializeField] private ResearchEffect[] effects;

        public FactionData FactionData => factionData;
        public ResearchEffect[] Effects => effects;
    }
}
