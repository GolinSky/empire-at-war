using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.TacticalMarkers;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class ClientTacticalMarkerReceiver : IInitializable, ILateDisposable
    {
        private readonly TacticalMarkerRegistry _markers;

        public ClientTacticalMarkerReceiver(TacticalMarkerRegistry markers) { _markers = markers; }

        public void Initialize()
        {
            NetworkClient.RegisterHandler<TacticalMarkerMessage>(Receive);
            NetworkClient.OnDisconnectedEvent += _markers.Clear;
        }

        private void Receive(TacticalMarkerMessage message)
        {
            if (message.State != TacticalMarkerState.Active)
                _markers.RemoveMirror(message.Id, message.State);
            else _markers.ApplyMirror(message.Id, message.Type, new PlayerId(message.SenderIndex), message.Point,
                message.TargetId, message.Age, message.Remaining);
        }

        public void LateDispose()
        {
            NetworkClient.UnregisterHandler<TacticalMarkerMessage>();
            NetworkClient.OnDisconnectedEvent -= _markers.Clear;
            _markers.Clear();
        }
    }
}
