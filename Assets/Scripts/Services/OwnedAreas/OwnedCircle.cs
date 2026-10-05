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

        /// <summary>True when a circle of <paramref name="margin"/> radius at the position overlaps this one.</summary>
        public bool Contains(Vector3 position, float margin)
        {
            Vector3 center = Transform.position;
            float x = position.x - center.x;
            float z = position.z - center.z;
            float reach = Radius + margin;
            return x * x + z * z <= reach * reach;
        }
    }
}
