using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>The units one enemy faction sends for a tutorial group.</summary>
    [Serializable]
    public sealed class TutorialFactionUnits
    {
        [SerializeField] private FactionType faction;
        [SerializeField] private SurvivalWaveEntry[] units = new SurvivalWaveEntry[0];

        public FactionType Faction => faction;
        public IReadOnlyList<SurvivalWaveEntry> Units => units;
    }
}
