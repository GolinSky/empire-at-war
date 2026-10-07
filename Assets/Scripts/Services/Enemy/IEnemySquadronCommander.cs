using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemySquadronCommander
    {
        /// <summary>Takes over orders for a station-launched AI squadron until it is released.</summary>
        void Command(ISquadron squadron, SquadronType squadronType);
    }
}
