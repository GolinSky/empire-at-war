using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct ObstacleSpot
    {
        public ObstacleSpot(Vector3 position, float yaw, float scale)
        {
            Position = position;
            Yaw = yaw;
            Scale = scale;
        }

        public Vector3 Position { get; }
        public float Yaw { get; }
        public float Scale { get; }
    }
}
