using EmpireAtWar.Services.Networking.Online;
using Mirror;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host → every remote client that has loaded the battle scene. Mirror disconnects a client on an unknown
    /// message, and battle handlers exist only once the battle scene is loaded: the host's own local client never
    /// registers them, and a reconnecting client is still loading.
    /// </summary>
    public static class HostBroadcast
    {
        public static void ToTeam<T>(OnlineMatchService online, INetworkPlayerDirectory players,
            IPlayerRelations relations, PlayerId sender, T message) where T : struct, NetworkMessage
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                if (online.IsBattleLoaded(connection) && players.TryGetPlayer(connection.connectionId, out PlayerId player) &&
                    relations.IsAllied(sender, player)) connection.Send(message);
        }

        public static void ToClients<T>(OnlineMatchService online, T message, int channelId = Channels.Reliable)
            where T : struct, NetworkMessage
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (online.IsBattleLoaded(connection)) connection.Send(message, channelId);
            }
        }
    }
}
