using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public readonly struct TacticalMarkerReply
    {
        public long MarkerId { get; }
        public PlayerId Sender { get; }
        public PlayerId Responder { get; }
        public TeamId Team { get; }
        public TacticalMarkerReplyStatus Status { get; }
        public Vector3 Point { get; }
        public TacticalMarkerType Type { get; }

        public TacticalMarkerReply(long markerId, PlayerId sender, PlayerId responder, TeamId team,
            TacticalMarkerReplyStatus status, Vector3 point, TacticalMarkerType type)
        {
            MarkerId = markerId;
            Sender = sender;
            Responder = responder;
            Team = team;
            Status = status;
            Point = point;
            Type = type;
        }
    }
}
