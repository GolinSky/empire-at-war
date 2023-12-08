using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.Terrain
{
    public interface ITerrainCommand:ICommand
    {
        ISelectionCommand SelectionCommand { get; }

    }
    public class TerrainCommand:Command<TerrainController>, ITerrainCommand
    {
        public ISelectionCommand SelectionCommand { get; }

        public TerrainCommand(TerrainController controller, IGameObserver gameObserver, INavigationService navigationService) : base(controller, gameObserver)
        {
            SelectionCommand = new SelectionCommand(controller, navigationService, controller);
        }

   
    }
}