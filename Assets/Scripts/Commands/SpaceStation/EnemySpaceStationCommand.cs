using EmpireAtWar.Controllers.SpaceStation;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SpaceStation
{
    public class EnemySpaceStationCommand : Command<SpaceStationController>, ISpaceStationCommand
    {
        public EnemySpaceStationCommand(SpaceStationController controller) : base(controller)
        {
        }
    }
}