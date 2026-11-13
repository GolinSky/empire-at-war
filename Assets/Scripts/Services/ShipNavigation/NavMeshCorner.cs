using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct NavMeshCorner
    {
        public readonly Vector3 Position;
        public readonly int DiskIndex;
        // Positive is counterclockwise in the XZ plane (negative Unity yaw).
        public readonly int TurnSign;
        public readonly float PortalWidth;

        public NavMeshCorner(Vector3 position, int diskIndex, int turnSign, float portalWidth)
        {
            Position = position;
            DiskIndex = diskIndex;
            TurnSign = turnSign;
            PortalWidth = portalWidth;
        }
    }
}
