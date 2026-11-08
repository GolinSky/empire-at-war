namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Network role of this machine in the current battle. Offline battles use <see cref="OfflineSession"/>:
    /// this machine is the authority and commands run immediately.
    /// </summary>
    public interface IMatchSession
    {
        bool IsOnline { get; }

        /// <summary>True on the machine that runs the simulation (offline, or the online host).</summary>
        bool IsAuthority { get; }

        /// <summary>The slot controlled by the human at this machine.</summary>
        PlayerId LocalPlayerId { get; }
    }
}
