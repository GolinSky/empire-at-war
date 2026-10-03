using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>One circle of the impassable ground footprint of an asteroid field.</summary>
    public readonly struct FieldVolume
    {
        public Vector3 Center { get; }
        public float Radius { get; }

        public FieldVolume(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
