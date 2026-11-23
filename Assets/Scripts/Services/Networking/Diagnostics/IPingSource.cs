namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>Round-trip time to the other machines of an online battle.</summary>
    public interface IPingSource
    {
        /// <summary>False in offline battles: there is no ping to show.</summary>
        bool IsOnline { get; }

        /// <summary>Client: RTT to the host. Host: the slowest client's RTT; false while no client is connected.</summary>
        bool TryGetPingMs(out int pingMs);
    }
}
