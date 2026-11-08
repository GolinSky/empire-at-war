using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host → every remote client. <c>NetworkServer.SendToAll</c> also reaches the host's own local client, which
    /// registers no replication handlers; Mirror disconnects a client on an unknown message.
    /// </summary>
    public static class HostBroadcast
    {
        public static void ToClients<T>(T message, int channelId = Channels.Reliable) where T : struct, NetworkMessage
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection != NetworkServer.localConnection) connection.Send(message, channelId);
            }
        }
    }
}
