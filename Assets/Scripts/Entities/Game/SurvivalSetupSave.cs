using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>The <see cref="SurvivalSettings"/> of a saved battle; JSON has no nullable, so the ally keeps a flag.</summary>
    [Serializable]
    public sealed class SurvivalSetupSave
    {
        [SerializeField] private FactionType playerFaction;
        [SerializeField] private bool hasAlly;
        [SerializeField] private FactionType allyFaction;
        [SerializeField] private FactionType enemyFaction;
        [SerializeField] private MapSize mapSize;
        [SerializeField] private EnemyAiDifficulty difficulty;
        [SerializeField] private bool superWeapons;

        public static SurvivalSetupSave From(SurvivalSettings settings) => new SurvivalSetupSave
        {
            playerFaction = settings.PlayerFaction,
            hasAlly = settings.AllyFaction.HasValue,
            allyFaction = settings.AllyFaction.GetValueOrDefault(),
            enemyFaction = settings.EnemyFaction,
            mapSize = settings.MapSize,
            difficulty = settings.Difficulty,
            superWeapons = settings.SuperWeapons
        };

        public BattleDefinition Rebuild() => SurvivalPreset.Create(new SurvivalSettings(playerFaction,
            hasAlly ? allyFaction : (FactionType?)null, enemyFaction, mapSize, difficulty, superWeapons)).Build();
    }
}
