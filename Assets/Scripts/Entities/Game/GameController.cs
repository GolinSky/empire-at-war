using EmpireAtWar.Services.Saves;
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
            Model.PendingBattleSave = null;
            Model.Battle = definition;
            _sceneService.LoadScene(SceneType.Battle);
        }

        public void LoadBattle(BattleSave save)
        {
            if (!save.Header.IsCompatible)
                throw new System.IO.InvalidDataException("This save version is not supported.");
            BattleDefinition definition = save.Setup.Rebuild();
            MatchRules.Validate(definition);
            if (definition.Mode != save.Header.Mode)
                throw new System.IO.InvalidDataException("The save header and battle mode do not match.");
            Model.Battle = definition;
            Model.PendingBattleSave = save;
            UnityEngine.Time.timeScale = 1f;
            _sceneService.LoadScene(SceneType.Battle);
        }

        public void ExitGame()
        {
            Model.PendingBattleSave = null;
            _sceneService.LoadScene(SceneType.MainMenu);
        }
    }
}
