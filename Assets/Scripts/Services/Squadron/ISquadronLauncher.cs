using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Squadrons
{
    public interface ISquadronLauncher
    {
        ISquadron LaunchFromStation(PlayerType playerType, SquadronType squadronType);
    }
}
