using EmpireAtWar.Services.TacticalMarkers;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    public struct TacticalMarkerReplyMessage : NetworkMessage
    {
        public long MarkerId;
        public int SenderIndex;
        public int ResponderIndex;
        public TacticalMarkerReplyStatus Status;
        public Vector3 Point;
        public TacticalMarkerType Type;
    }
}
