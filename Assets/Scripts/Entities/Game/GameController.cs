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
        private readonly BattleSaveValidator _saveValidator;

        public GameController(ISceneService sceneService, GameData model,
            BattleSaveValidator saveValidator = null) : base(model)
        {
            _sceneService = sceneService;
            _saveValidator = saveValidator;
        }

        public void StartBattle(BattleDefinition definition)
        {
            MatchRules.Validate(definition);
            Model.PendingBattleSave = null;
            Model.PendingLoadError = null;
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
            if (_saveValidator != null) _saveValidator.Validate(save, definition);
            Model.Battle = definition;
            Model.PendingBattleSave = save;
            Model.PendingLoadError = null;
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
