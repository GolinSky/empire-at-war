using Mirror;
using Mirror.FizzySteam;
using Steamworks;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>Reads live Steam link status for a connection. KCP (direct IP) links have none.</summary>
    public static class NetworkLinkProbe
    {
        /// <summary>Host: the transport serving a Mirror connection (a Steam lobby host listens on KCP and Steam at once).</summary>
        public static Transport ServerTransport(int connectionId, out int transportConnectionId)
        {
            transportConnectionId = connectionId;
            if (Transport.active is MultiplexTransport multiplex &&
                multiplex.OriginalId(connectionId, out int originalId, out int transportIndex))
            {
                transportConnectionId = originalId;
                return multiplex.transports[transportIndex];
            }
            return Transport.active;
        }

        public static bool TryGetServerSteamStatus(int connectionId, out SteamNetConnectionRealTimeStatus_t status)
        {
            status = default;
            return ServerTransport(connectionId, out int transportConnectionId) is FizzySteamworks fizzy &&
                fizzy.TryGetServerConnectionStatus(transportConnectionId, out status);
        }

        public static bool TryGetClientSteamStatus(out SteamNetConnectionRealTimeStatus_t status)
        {
            status = default;
            return Transport.active is FizzySteamworks fizzy && fizzy.TryGetClientConnectionStatus(out status);
        }
    }
}
