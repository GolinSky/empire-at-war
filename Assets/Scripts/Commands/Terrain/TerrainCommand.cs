using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.Terrain
{
    public interface ITerrainCommand:ICommand
    {

    }
    public class TerrainCommand:Command<TerrainController>, ITerrainCommand
    {

        public TerrainCommand(TerrainController controller, IGameObserver gameObserver, INavigationService navigationService) : base(controller, gameObserver)
        {
            AddCommand(
                new SelectionCommand(controller, navigationService, controller)
            );
        }
    }
}