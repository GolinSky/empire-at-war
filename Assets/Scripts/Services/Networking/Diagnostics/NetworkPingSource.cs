using System;
using EmpireAtWar.Services.Networking.Online;
using Mirror;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>Reads Mirror's smoothed RTT for the local machine's battle role.</summary>
    public sealed class NetworkPingSource : IPingSource
    {
        private readonly OnlineRole _role;

        public NetworkPingSource(OnlineRole role)
        {
            _role = role;
        }

        public bool IsOnline => _role != OnlineRole.None;

        public bool TryGetPingMs(out int pingMs)
        {
            pingMs = 0;
            switch (_role)
            {
                case OnlineRole.Client:
                    pingMs = ToMs(NetworkTime.rtt);
                    return true;
                case OnlineRole.Host:
                    return TryGetSlowestClientMs(out pingMs);
                default:
                    return false;
            }
        }

        private static bool TryGetSlowestClientMs(out int pingMs)
        {
            pingMs = 0;
            bool hasClient = false;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == NetworkServer.localConnection) continue;
                pingMs = Math.Max(pingMs, ToMs(connection.rtt));
                hasClient = true;
            }
            return hasClient;
        }

        private static int ToMs(double rttSeconds) => (int)Math.Round(rttSeconds * 1000.0);
    }
}
