using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class PlacementThreatSnapshot
    {
        private readonly List<(float X, float Z, float Radius)> _threats = new List<(float X, float Z, float Radius)>();

        private (float X, float Z)? _approach;

        public void Clear()
        {
            _threats.Clear();
            _approach = null;
        }

        public void SetApproach(float x, float z) => _approach = (x, z);

        public void Add(float x, float z, float radius) => _threats.Add((x, z, radius));

        public (float X, float Z) GetLocalCentroid(float x, float z, float searchRadius)
        {
            float sumX = 0f;
            float sumZ = 0f;
            int count = 0;
            foreach (var threat in _threats)
            {
                float dx = x - threat.X;
                float dz = z - threat.Z;
                float reach = threat.Radius + searchRadius;
                if (dx * dx + dz * dz > reach * reach) continue;
                sumX += threat.X;
                sumZ += threat.Z;
                count++;
            }

            return count > 0 ? (sumX / count, sumZ / count) : _approach ?? (x, z);
        }

        /// <summary>True when a threat's centre lies within <paramref name="distance"/> of the point.</summary>
        public bool HasThreatWithin(float x, float z, float distance)
        {
            foreach (var threat in _threats)
            {
                float dx = x - threat.X;
                float dz = z - threat.Z;
                if (dx * dx + dz * dz <= distance * distance) return true;
            }

            return false;
        }

        public bool IsSafe(float x, float z, float clearance)
        {
            foreach (var threat in _threats)
            {
                float dx = x - threat.X;
                float dz = z - threat.Z;
                // Compare the rounded distance to preserve the legacy inclusive boundary.
                if ((float)Math.Sqrt(dx * dx + dz * dz) <= threat.Radius + clearance)
                    return false;
            }

            return true;
        }
    }
}
