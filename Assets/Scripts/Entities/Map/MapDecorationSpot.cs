using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct MapDecorationSpot
    {
        public GameObject Prefab { get; }
        public Vector3 Position { get; }
        public float Yaw { get; }
        public float Scale { get; }
        public float Radius { get; }

        public MapDecorationSpot(GameObject prefab, Vector3 position, float yaw, float scale, float radius)
        {
            Prefab = prefab;
            Position = position;
            Yaw = yaw;
            Scale = scale;
            Radius = radius;
        }
    }
}
