using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [Serializable]
    public sealed class SurvivalWave
    {
        [SerializeField] private SurvivalWaveEntry[] entries;
        [SerializeField, Min(1)] private int spawnGroupCount = 1;
        public IReadOnlyList<SurvivalWaveEntry> Entries => entries;
        public int SpawnGroupCount => spawnGroupCount;
        public SurvivalWave(SurvivalWaveEntry[] units, int groups) { entries = units; spawnGroupCount = groups; }
    }
}
