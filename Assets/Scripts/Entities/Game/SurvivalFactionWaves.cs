using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [Serializable]
    public sealed class SurvivalFactionWaves
    {
        [SerializeField] private FactionType faction;
        [SerializeField] private SurvivalWave[] waves;
        public FactionType Faction => faction;
        public IReadOnlyList<SurvivalWave> Waves => waves;
        public SurvivalFactionWaves(FactionType type, SurvivalWave[] values) { faction = type; waves = values; }
    }
}
