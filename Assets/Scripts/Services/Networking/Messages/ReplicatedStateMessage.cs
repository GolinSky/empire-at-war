using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: the latest JSON of one <see cref="IReplicatedState"/>.</summary>
    public struct ReplicatedStateMessage : NetworkMessage
    {
        public string Key;
        public string Json;
    }
}
