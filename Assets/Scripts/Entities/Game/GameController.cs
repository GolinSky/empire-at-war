using System;
using EmpireAtWar.Commands.Game;
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

        public void StartBattle(BattleDefinition definition)
        {
            MatchRules.Validate(definition);
            if (definition.Outcome != OutcomeRule.Skirmish)
                throw new NotSupportedException("This battle's gameplay is not available yet.");
            Model.Battle = definition;
            _sceneService.LoadScene(SceneType.Battle);
        }

        public void ExitGame()
        {
            _sceneService.LoadScene(SceneType.MainMenu);
        }
    }
}
