using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Game
{
    public class GameController : Controller<GameData>, IGameCommand
    {
        private readonly ISceneService _sceneService;

        public GameController(ISceneService sceneService, GameData model) : base(model)
        {
            _sceneService = sceneService;
        }

        public void StartGame(
            IReadOnlyList<PlayerSlot> players,
            PlanetType planetType,
            MapSize mapSize,
            BattleVictoryCondition victoryCondition,
            float startingMoney)
        {
            MatchRules.Validate(players);
            if (startingMoney <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoney), "Starting money must be greater than zero.");
            }

            Model.Players = players;
            Model.PlanetType = planetType;
            Model.MapSize = mapSize;
            Model.VictoryCondition = victoryCondition;
            Model.StartingMoney = startingMoney;
            Model.GameMode = GameMode.Skirmish;
            _sceneService.LoadScene(SceneType.Battle);
        }

        public void ExitGame()
        {
            _sceneService.LoadScene(SceneType.MainMenu);
        }
    }
}
