using System;
using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Components.Hangar
{
    public interface IHangarLaunchEvents
    {
        event Action<ISquadron> SquadronLaunched;
    }
}
