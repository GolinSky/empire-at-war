using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.TacticalMarkers;

namespace EmpireAtWar.Services.Networking
{
    public sealed class HostTacticalMarkerReplySender : IInitializable, ILateDisposable
    {
        private readonly TacticalMarkerReplies _replies;
        private readonly OnlineMatchService _online;
        private readonly INetworkPlayerDirectory _players;
        private readonly IPlayerRelations _relations;

        public HostTacticalMarkerReplySender(TacticalMarkerReplies replies, OnlineMatchService online,
            INetworkPlayerDirectory players, IPlayerRelations relations)
        {
            _replies = replies;
            _online = online;
            _players = players;
            _relations = relations;
        }

        public void Initialize() => _replies.Replied += Send;
        public void LateDispose() => _replies.Replied -= Send;

        private void Send(TacticalMarkerReply reply) => HostBroadcast.ToTeam(_online, _players, _relations,
            reply.Sender, new TacticalMarkerReplyMessage
            {
                MarkerId = reply.MarkerId, SenderIndex = reply.Sender.Index, ResponderIndex = reply.Responder.Index,
                Status = reply.Status, Point = reply.Point, Type = reply.Type
            });
    }
}
