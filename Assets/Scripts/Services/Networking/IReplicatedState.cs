using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// System state the host sends to clients as JSON whenever it changes (<see cref="HostReplicatedStateSender"/>).
    /// A client applies it instead of simulating the system itself. Registered in every battle; offline it is idle.
    /// </summary>
    public interface IReplicatedState
    {
        /// <summary>Same on host and client, unique per battle.</summary>
        string Key { get; }

        /// <summary>The only player who receives it; <see cref="PlayerId.None"/> = everyone.</summary>
        PlayerId Audience { get; }

        string Capture();

        void Apply(string json);
    }
}
