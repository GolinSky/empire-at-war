using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    public interface IGameModelObserver : IModelObserver
    {
        PlanetType PlanetType { get; }
        MapSize MapSize { get; }
        IReadOnlyList<PlayerSlot> Players { get; }
        BattleVictoryCondition VictoryCondition { get; }
        float StartingMoney { get; }
    }

    [CreateAssetMenu(fileName = nameof(GameData), menuName = "Data/GameData")]
    public class GameData : Data, IModel, IGameModelObserver
    {
        public GameMode GameMode { get; set; }
        public PlanetType PlanetType { get; set; }
        public MapSize MapSize { get; set; }
        // Opening a battle scene directly in the Editor skips the setup screen, so default to a 1v1.
        public IReadOnlyList<PlayerSlot> Players { get; set; } = CreateDefaultDuel();
        public BattleVictoryCondition VictoryCondition { get; set; } = BattleVictoryCondition.DestroyEnemyFleet;
        public float StartingMoney { get; set; } = 1000f;

        private static IReadOnlyList<PlayerSlot> CreateDefaultDuel()
        {
            return new[]
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
        }
    }
}
