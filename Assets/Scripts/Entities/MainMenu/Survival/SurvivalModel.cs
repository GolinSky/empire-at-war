using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public sealed class SurvivalModel
    {
        private const int NO_ALLY = -1;

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

        public SurvivalLobbySnapshot CaptureLobby() => new SurvivalLobbySnapshot
        {
            playerFaction = (int)PlayerFaction,
            allyFaction = AllyFaction.HasValue ? (int)AllyFaction.Value : NO_ALLY,
            enemyFaction = (int)EnemyFaction,
            mapSize = (int)MapSize,
            difficulty = (int)Difficulty,
            superWeapons = SuperWeapons
        };

        /// <summary>Lobby client: shows the host's setup.</summary>
        public void ApplyLobby(SurvivalLobbySnapshot snapshot)
        {
            PlayerFaction = (FactionType)snapshot.playerFaction;
            AllyFaction = snapshot.allyFaction == NO_ALLY ? null : (FactionType)snapshot.allyFaction;
            EnemyFaction = (FactionType)snapshot.enemyFaction;
            MapSize = (MapSize)snapshot.mapSize;
            Difficulty = (EnemyAiDifficulty)snapshot.difficulty;
            SuperWeapons = snapshot.superWeapons;
            Changed?.Invoke();
        }
    }
}
