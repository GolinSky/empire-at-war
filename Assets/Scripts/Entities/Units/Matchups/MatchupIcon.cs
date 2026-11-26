using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Units.Matchups
{
    /// <summary>A faction's representative icon for a matchup group; tooltips hide the viewed unit's own faction.</summary>
    [Serializable]
    public struct MatchupIcon
    {
        [SerializeField] private FactionType faction;
        [SerializeField] private string iconKey;

        public FactionType Faction => faction;
        public string IconKey => iconKey;

        public MatchupIcon(FactionType faction, string iconKey)
        {
            this.faction = faction;
            this.iconKey = iconKey;
        }
    }
}
