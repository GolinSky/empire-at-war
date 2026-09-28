using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Squadrons
{
    public interface ISquadronLauncher
    {
        ISquadron LaunchFromStation(PlayerId owner, SquadronType squadronType);
    }
}
