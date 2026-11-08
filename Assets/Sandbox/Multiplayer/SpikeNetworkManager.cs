using System;
using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Thin NetworkManager: no player prefab, no scene management. Reports lifecycle to MultiplayerSpike.
    public sealed class SpikeNetworkManager : NetworkManager
    {
        public event Action ServerStarted;
        public event Action ServerStopped;
        public event Action<NetworkConnectionToClient> ServerClientConnected;
        public event Action<NetworkConnectionToClient> ServerClientDisconnected;
        public event Action ClientStarted;
        public event Action ClientStopped;

        public void UseTransport(Transport selected)
        {
            transport = selected;
            Transport.active = selected;
        }

        public override void OnStartServer() => ServerStarted?.Invoke();
        public override void OnStopServer() => ServerStopped?.Invoke();
        public override void OnServerConnect(NetworkConnectionToClient conn) => ServerClientConnected?.Invoke(conn);

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ServerClientDisconnected?.Invoke(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnStartClient() => ClientStarted?.Invoke();
        public override void OnStopClient() => ClientStopped?.Invoke();
    }
}
