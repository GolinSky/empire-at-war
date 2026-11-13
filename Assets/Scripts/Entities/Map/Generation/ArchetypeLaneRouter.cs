using System.Collections.Generic;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Cross-belt routes use the planet's stable passes before field rasterisation carves their corridors.</summary>
    public static class ArchetypeLaneRouter
    {
        public static void Route(MapSizeSettings size, List<MapLane> lanes)
        {
            if (size.Archetype != MapArchetypeKind.AsteroidBelt && size.Archetype != MapArchetypeKind.TwinPass)
                return;
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            Quaternion rotation = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            Quaternion inverse = Quaternion.Inverse(rotation);
            float half = Mathf.Min(size.Bounds.Max.x - size.Bounds.Min.x,
                size.Bounds.Max.y - size.Bounds.Min.y) * 0.5f;
            bool belt = size.Archetype == MapArchetypeKind.AsteroidBelt;
            float band = half * (belt ? 0.22f : 0.28f);
            float flank = half * (belt ? 0.62f : 0.72f);
            float approach = band + size.MinLaneWidth * 0.5f + size.FieldCellSize;
            for (int i = 0; i < lanes.Count; i++)
            {
                MapLane lane = lanes[i];
                Vector3 first = inverse * (lane.Points[0] - center);
                Vector3 last = inverse * (lane.Points[lane.Points.Count - 1] - center);
                bool firstInside = Mathf.Abs(first.z) < band;
                bool lastInside = Mathf.Abs(last.z) < band;
                if (belt && (first.z * last.z >= 0f || firstInside || lastInside)) continue;
                if (!belt && (firstInside && lastInside ||
                    !firstInside && !lastInside && first.z * last.z >= 0f)) continue;
                float firstZ = firstInside ? first.z : Mathf.Sign(first.z) * Mathf.Min(approach, Mathf.Abs(first.z));
                float lastZ = lastInside ? last.z : Mathf.Sign(last.z) * Mathf.Min(approach, Mathf.Abs(last.z));
                float gap = -flank;
                float shortest = GetLength(first, last, firstZ, lastZ, gap);
                float positive = GetLength(first, last, firstZ, lastZ, flank);
                if (positive < shortest) { gap = flank; shortest = positive; }
                if (belt && GetLength(first, last, firstZ, lastZ, 0f) < shortest) gap = 0f;
                lanes[i] = new MapLane(new[]
                {
                    lane.Points[0], center + rotation * new Vector3(gap, 0f, firstZ),
                    center + rotation * new Vector3(gap, 0f, lastZ), lane.Points[lane.Points.Count - 1]
                });
            }
        }
        private static float GetLength(Vector3 first, Vector3 last, float firstZ, float lastZ, float gap)
        {
            return Vector3.Distance(first, new Vector3(gap, 0f, firstZ)) + Mathf.Abs(firstZ - lastZ) +
                Vector3.Distance(new Vector3(gap, 0f, lastZ), last);
        }
    }
}
