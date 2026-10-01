using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Ship
{
    public class ShipModel : IModel, IShipModelObserver
    {
        public ShipType ShipType { get; }

        public ShipModel(ShipType shipType)
        {
            ShipType = shipType;
        }
    }
}
