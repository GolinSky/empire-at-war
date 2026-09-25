using EmpireAtWar.Ship;

namespace EmpireAtWar.Entities.Squadrons
{
    public interface ISquadronModelObserver : IUnitModelObserver
    {
        SquadronType SquadronType { get; }
    }
}
