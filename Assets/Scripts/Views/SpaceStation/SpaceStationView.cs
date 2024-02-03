using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Views.SpaceStation
{
    public class SpaceStationView : View<ISpaceStationModelObserver, ISpaceStationCommand>
    {
        protected override void OnInitialize()
        {
            transform.position = Model.StartPosition;
        }

        protected override void OnDispose()
        {
        }
    }
}