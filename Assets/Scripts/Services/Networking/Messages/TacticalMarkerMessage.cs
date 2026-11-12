using EmpireAtWar.Services.TacticalMarkers;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    public struct TacticalMarkerMessage : NetworkMessage
    {
        public long Id;
        public TacticalMarkerType Type;
        public int SenderIndex;
        public Vector3 Point;
        public long TargetId;
        public float Age;
        public float Remaining;
        public TacticalMarkerState State;
    }
}
