using System.Collections.Generic;
using EmpireAtWar.Presenters.ReinforcementZones;

namespace EmpireAtWar.Services.ReinforcementZones
{
    /// <summary>Read access to the live reinforcement zones.</summary>
    public interface IReinforcementZoneSource
    {
        IReadOnlyList<ReinforcementZonePresenter> Zones { get; }
    }
}
