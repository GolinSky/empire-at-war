using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class TacticalMarker
    {
        public long Id { get; }
        public TacticalMarkerType Type { get; }
        public PlayerId Sender { get; }
        public TeamId Team { get; }
        public Vector3 Point { get; internal set; }
        public long TargetId { get; internal set; }
        public float CreatedAt { get; internal set; }
        public float ExpiresAt { get; internal set; }
        public TacticalMarkerState State { get; internal set; }

        public TacticalMarker(long id, TacticalMarkerType type, PlayerId sender, TeamId team, Vector3 point,
            long targetId, float createdAt, float expiresAt)
        {
            Id = id;
            Type = type;
            Sender = sender;
            Team = team;
            Point = point;
            TargetId = targetId;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }
    }
}
