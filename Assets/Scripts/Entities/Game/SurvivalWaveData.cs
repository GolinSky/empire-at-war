using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [CreateAssetMenu(fileName = nameof(SurvivalWaveData), menuName = "Data/Battles/Survival Waves")]
    public sealed class SurvivalWaveData : ScriptableObject
    {
        [SerializeField] private float firstPreparationSeconds = 120f;
        [SerializeField] private float preparationSeconds = 60f;
        [SerializeField] private SurvivalFactionWaves[] factions;
        public float FirstPreparationSeconds => firstPreparationSeconds;
        public float PreparationSeconds => preparationSeconds;
        public IReadOnlyList<SurvivalFactionWaves> Factions => factions;

        public SurvivalWave GetWave(FactionType faction, int index)
        {
            foreach (SurvivalFactionWaves entry in factions)
                if (entry.Faction == faction) return entry.Waves[index - 1];
            throw new InvalidOperationException($"No Survival waves configured for {faction}.");
        }
    }
}
