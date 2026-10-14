using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public sealed class SurvivalModel
    {
        public event Action Changed;
        public FactionType PlayerFaction { get; private set; } = FactionType.Republic;
        public FactionType? AllyFaction { get; private set; }
        public FactionType EnemyFaction { get; private set; } = FactionType.Separatist;
        public MapSize MapSize { get; private set; } = MapSize.Small;
        public EnemyAiDifficulty Difficulty { get; private set; } = EnemyAiDifficulty.Medium;
        public bool SuperWeapons { get; private set; } = true;

        public void SelectPlayer(FactionType value) { PlayerFaction = value; Changed?.Invoke(); }
        public void SelectAlly(FactionType? value) { AllyFaction = value; Changed?.Invoke(); }
        public void SelectEnemy(FactionType value) { EnemyFaction = value; Changed?.Invoke(); }
        public void SelectMapSize(MapSize value) { MapSize = value; Changed?.Invoke(); }
        public void SelectDifficulty(EnemyAiDifficulty value) { Difficulty = value; Changed?.Invoke(); }
        public void SelectSuperWeapons(bool value) { SuperWeapons = value; Changed?.Invoke(); }
        public BattleDefinition CreateBattle() => SurvivalPreset.Create(new SurvivalSettings(
            PlayerFaction, AllyFaction, EnemyFaction, MapSize, Difficulty, SuperWeapons)).Build();
    }
}
