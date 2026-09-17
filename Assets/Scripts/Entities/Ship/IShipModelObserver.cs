using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Ship
{
    public interface IShipModelObserver : IUnitModelObserver
    {
        ShipType ShipType { get; }
    }
}
