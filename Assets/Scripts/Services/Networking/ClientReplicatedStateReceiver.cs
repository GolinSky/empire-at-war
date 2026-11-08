using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: applies the host's <see cref="ReplicatedStateMessage"/>s through <see cref="ReplicatedStateRegistry"/>.</summary>
    public sealed class ClientReplicatedStateReceiver : IInitializable, ILateDisposable
    {
        private readonly ReplicatedStateRegistry _states;

        public ClientReplicatedStateReceiver(ReplicatedStateRegistry states)
        {
            _states = states;
        }

        public void Initialize() => NetworkClient.RegisterHandler<ReplicatedStateMessage>(OnState);

        public void LateDispose() => NetworkClient.UnregisterHandler<ReplicatedStateMessage>();

        private void OnState(ReplicatedStateMessage message) => _states.Apply(message.Key, message.Json);
    }
}
