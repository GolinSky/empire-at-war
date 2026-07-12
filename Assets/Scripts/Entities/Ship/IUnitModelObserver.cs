using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Ship
{
    public interface IUnitModelObserver:IModelObserver
    {
        IHealthModelObserver HealthModel { get; }
    }
}
