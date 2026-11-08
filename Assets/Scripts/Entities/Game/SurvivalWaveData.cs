using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [CreateAssetMenu(fileName = nameof(SurvivalWaveData), menuName = "Data/Battles/Survival Waves")]
    public sealed class SurvivalWaveData : Data
    {
        [SerializeField] private float firstPreparationSeconds = 120f;
        [SerializeField] private float preparationSeconds = 60f;
        [Tooltip("Extra share of every wave entry per defender beyond the first (co-op human or AI ally).")]
        [SerializeField] private float extraDefenderWaveShare = 0.5f;
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

        /// <summary>A wave entry's unit count for <paramref name="defenders"/> defending players, rounded up.</summary>
        public int ScaleQuantity(int quantity, int defenders) =>
            Mathf.CeilToInt(quantity * (1f + extraDefenderWaveShare * (defenders - 1)));
    }
}
