using Steamworks;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>One connection's link numbers in one window (host: one per client; client: the host link).</summary>
    public readonly struct NetworkLinkSample
    {
        public NetworkLinkSample(string label, double rttMs, bool hasSteam, SteamNetConnectionRealTimeStatus_t status)
        {
            Label = label;
            RttMs = rttMs;
            HasSteam = hasSteam;
            Status = status;
        }

        public string Label { get; }
        public double RttMs { get; }
        /// <summary>False on KCP (direct IP) links: <see cref="Status"/> is empty.</summary>
        public bool HasSteam { get; }
        public SteamNetConnectionRealTimeStatus_t Status { get; }
    }
}
