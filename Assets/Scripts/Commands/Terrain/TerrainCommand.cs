using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Terrain
{
    public interface ITerrainCommand:ICommand
    {

    }
    public class TerrainCommand:Command<TerrainController>, ITerrainCommand
    {

        public TerrainCommand(TerrainController controller, INavigationService navigationService) : base(controller)
        {
            AddCommand(
                new SelectionCommand(controller, navigationService, controller)
            );
        }
    }
}