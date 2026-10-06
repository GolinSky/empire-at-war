using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Squadrons
{
    public interface ISquadronLauncher
    {
        /// <summary>Returns false when the owner has no living station with a hangar, e.g. it fell mid-build.</summary>
        bool TryLaunchFromStation(PlayerId owner, SquadronType squadronType, out ISquadron squadron);
    }
}
