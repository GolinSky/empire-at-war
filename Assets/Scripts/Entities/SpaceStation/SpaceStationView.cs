using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ViewImpl;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationView : View<ISpaceStationModelObserver, ISpaceStationCommand>
    {
        
        [Inject] private PlayerType PlayerType { get; }
        protected override void OnInitialize()
        {
            gameObject.name = $"{PlayerType}_SpaceStation";
        }
        

        protected override void OnDispose()
        {
        }
    }
}