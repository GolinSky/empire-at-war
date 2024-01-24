using EmpireAtWar.Services.SceneService;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.LightWeightFramework.PopupCommands
{
    public interface ISkirmishSetupCommand:ICommand
    {
        void StartGame();
    }

    public class SkirmishSetUpCommand : ISkirmishSetupCommand
    {
        private readonly ISceneService sceneService;

        public SkirmishSetUpCommand(ISceneService sceneService)
        {
            this.sceneService = sceneService;
        }
        
        public void StartGame()
        {
            sceneService.LoadScene(SceneType.Skirmish);
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            command = default;
            return false;
        }
    }
}