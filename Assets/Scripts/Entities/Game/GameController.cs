using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Game
{
    public class GameController : Controller<GameModel>, IGameCommand
    {
        private readonly ISceneService _sceneService;

        public GameController(GameModel model, ISceneService sceneService) : base(model)
        {
            _sceneService = sceneService;
        }
        
        public void StartGame(
            FactionType playerFactionType,
            FactionType enemyFactionType,
            PlanetType planetType,
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty enemyDifficulty,
            float startingMoney)
        {
            if (playerFactionType == enemyFactionType)
            {
                throw new ArgumentException(
                    "Player and enemy factions must be different because each map has one station anchor per faction.",
                    nameof(enemyFactionType));
            }

            if (startingMoney <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoney), "Starting money must be greater than zero.");
            }

            Model.EnemyFactionType = enemyFactionType;
            Model.PlayerFactionType = playerFactionType;
            Model.PlanetType = planetType;
            Model.VictoryCondition = victoryCondition;
            Model.EnemyDifficulty = enemyDifficulty;
            Model.StartingMoney = startingMoney;
            Model.GameMode = GameMode.Skirmish;
            _sceneService.LoadSceneByPlanetType(planetType);
        }

        public void ExitGame()
        {
            _sceneService.LoadScene(SceneType.MainMenu);
        }
    }
}
