using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Views.MiningFacility
{
    public class MiningFacilityView : View<IMiningFacilityModelObserver, IMiningFacilityCommand>
    {
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
        }
    }
}