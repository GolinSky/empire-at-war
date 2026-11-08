using System;
using Mirror;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Thin NetworkManager: no player prefab, no scene management; reports lifecycle to <see cref="OnlineMatchService"/>.</summary>
    public sealed class OnlineNetworkManager : NetworkManager
    {
        public event Action ServerStarted;
        public event Action<NetworkConnectionToClient> ServerClientConnected;
        public event Action<NetworkConnectionToClient> ServerClientDisconnected;
        public event Action ClientStarted;
        public event Action ClientDisconnected;

        public void UseTransport(Transport selected)
        {
            transport = selected;
            Transport.active = selected;
        }

        public override void OnStartServer() => ServerStarted?.Invoke();
        public override void OnServerConnect(NetworkConnectionToClient conn) => ServerClientConnected?.Invoke(conn);

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ServerClientDisconnected?.Invoke(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnStartClient() => ClientStarted?.Invoke();

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            ClientDisconnected?.Invoke();
        }
    }
}
