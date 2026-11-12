using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.TacticalMarkers;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class ClientTacticalMarkerReplyReceiver : IInitializable, ILateDisposable
    {
        private readonly TacticalMarkerReplies _replies;
        private readonly IPlayerRoster _players;

        public ClientTacticalMarkerReplyReceiver(TacticalMarkerReplies replies, IPlayerRoster players)
        {
            _replies = replies;
            _players = players;
        }

        public void Initialize() => NetworkClient.RegisterHandler<TacticalMarkerReplyMessage>(Receive);
        public void LateDispose() => NetworkClient.UnregisterHandler<TacticalMarkerReplyMessage>();

        private void Receive(TacticalMarkerReplyMessage message)
        {
            PlayerId sender = new PlayerId(message.SenderIndex);
            _replies.Receive(new TacticalMarkerReply(message.MarkerId, sender, new PlayerId(message.ResponderIndex),
                _players.Get(sender).Team, message.Status, message.Point, message.Type));
        }
    }
}
