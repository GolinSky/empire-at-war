using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class TacticalMarkerReplies
    {
        private readonly IMatchSession _session;
        private readonly IPlayerRoster _players;
        public event Action<TacticalMarkerReply> Replied;

        public TacticalMarkerReplies(IMatchSession session, IPlayerRoster players)
        {
            _session = session;
            _players = players;
        }

        public void Reply(TacticalMarker marker, PlayerId responder, TacticalMarkerReplyStatus status)
        {
            if (!_session.IsAuthority || !_players.IsAllied(marker.Sender, responder)) return;
            Replied?.Invoke(new TacticalMarkerReply(marker.Id, marker.Sender, responder, marker.Team,
                status, marker.Point, marker.Type));
        }

        public void CaptureCompleted(PlayerId owner)
        {
            if (!_session.IsAuthority) return;
            Replied?.Invoke(new TacticalMarkerReply(-1, owner, owner, _players.Get(owner).Team,
                TacticalMarkerReplyStatus.Captured, Vector3.zero, TacticalMarkerType.Capture));
        }

        public void Receive(TacticalMarkerReply reply)
        {
            if (_session.IsAuthority || !_players.IsAllied(_session.LocalPlayerId, reply.Sender)) return;
            Replied?.Invoke(reply);
        }
    }
}
