using EmpireAtWar.Components.Ship.Selection;
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

        public TerrainCommand(TerrainController controller, SelectionFacade selectionFacade) : base(controller)
        {
            // AddCommand(
            //     new SelectionCommand(controller, navigationService, controller)
            // );
            
            AddCommand(
                selectionFacade.Create(controller.GetModel(), controller.Movable)
            );
            
        }
    }
}