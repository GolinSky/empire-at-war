using EmpireAtWar.Commands.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Game
{
    public class GameController:Controller<GameModel>, IGameCommand
    {
        private readonly ISceneService sceneService;

        public GameController(GameModel model, ISceneService sceneService) : base(model)
        {
            this.sceneService = sceneService;
        }

        public void StartGame(FactionType playerFactionType, FactionType enemyFactionType)
        {
            Model.EnemyFactionType = enemyFactionType;
            Model.PlayerFactionType = playerFactionType;
            sceneService.LoadScene(SceneType.Skirmish);
        }
    }
}