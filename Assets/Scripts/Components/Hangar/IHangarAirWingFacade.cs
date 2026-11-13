using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>Read-only view of the squadrons a hangar has out, for AI strength and carrier role decisions.</summary>
    public interface IHangarAirWingFacade : IEntityFacade
    {
        /// <summary>The hangar can still launch: it is operational and its owner is alive.</summary>
        bool CanLaunch { get; }

        /// <summary>Squadrons launched from this hangar's reserve that are still alive.</summary>
        int LaunchedCount { get; }

        ISquadron GetLaunched(int index);
    }
}
