using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.OwnedAreas
{
    /// <summary>A flat (XZ) circle that follows a transform and belongs to one player.</summary>
    public sealed class OwnedCircle
    {
        public PlayerId Owner { get; set; }
        public Transform Transform { get; }
        public float Radius { get; set; }

        public OwnedCircle(PlayerId owner, Transform transform, float radius)
        {
            Owner = owner;
            Transform = transform;
            Radius = radius;
        }

        public bool Contains(Vector3 position)
        {
            Vector3 center = Transform.position;
            float x = position.x - center.x;
            float z = position.z - center.z;
            return x * x + z * z <= Radius * Radius;
        }
    }
}
