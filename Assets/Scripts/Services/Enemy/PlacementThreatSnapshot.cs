using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class PlacementThreatSnapshot
    {
        private readonly List<(float X, float Z, float Radius)> _threats = new List<(float X, float Z, float Radius)>();

        public void Clear() => _threats.Clear();

        public void Add(float x, float z, float radius) => _threats.Add((x, z, radius));

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
