using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Entities.Game
{
    public class GameController : Controller<GameModel>, IGameCommand
    {
        private readonly ISceneService _sceneService;

        public GameController(GameModel model, ISceneService sceneService) : base(model)
        {
            _sceneService = sceneService;
        }
        
        public void StartGame(FactionType playerFactionType, FactionType enemyFactionType, PlanetType planetType)
        {
            Model.EnemyFactionType = enemyFactionType;
            Model.PlayerFactionType = playerFactionType;
            Model.PlanetType = planetType;
            Model.GameMode = GameMode.Skirmish;
            _sceneService.LoadSceneByPlanetType(planetType);
        }

        public void ExitGame()
        {
            _sceneService.LoadScene(SceneType.MainMenu);
        }
    }
}