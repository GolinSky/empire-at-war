using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationView : View<ISpaceStationModelObserver, ISpaceStationCommand>
    {
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
        }
    }
}