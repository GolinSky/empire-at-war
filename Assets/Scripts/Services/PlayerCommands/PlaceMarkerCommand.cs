using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.TacticalMarkers;
using UnityEngine;

namespace EmpireAtWar.Services.PlayerCommands
{
    public readonly struct PlaceMarkerCommand
    {
        public PlayerId Issuer { get; }
        public TacticalMarkerType Type { get; }
        public Vector3 Point { get; }
        public long TargetId { get; }

        public PlaceMarkerCommand(PlayerId issuer, TacticalMarkerType type, Vector3 point,
            long targetId = TacticalMarkerTarget.NO_TARGET)
        {
            Issuer = issuer;
            Type = type;
            Point = point;
            TargetId = targetId;
        }
    }
}
