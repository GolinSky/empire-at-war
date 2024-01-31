using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Views.Ship
{
    public class ShipView : View<IShipModelObserver, IShipCommand>
    {
        
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
        }
    }
}