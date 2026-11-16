using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    /// <summary>Static, broad cluster occlusion from world-space rock bounds; calculated only during configuration.</summary>
    public static class DecorAsteroidOcclusion
    {
        private const float MIN_VISIBILITY = 0.45f;
        private const float MAX_OCCLUSION = 1f / MIN_VISIBILITY - 1f;

        public static float[] Calculate(IReadOnlyList<Bounds> bounds)
        {
            float[] radii = new float[bounds.Count];
            float[] visibility = new float[bounds.Count];
            float maxRadius = 0f;
            for (int i = 0; i < bounds.Count; i++)
            {
                radii[i] = bounds[i].extents.magnitude;
                maxRadius = Mathf.Max(maxRadius, radii[i]);
            }

            // Influence ends at three combined radii, at most six times the largest radius.
            float cellSize = Mathf.Max(1f, 6f * maxRadius);
            Dictionary<Vector3Int, List<int>> cells = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < bounds.Count; i++)
            {
                Vector3Int key = Vector3Int.FloorToInt(bounds[i].center / cellSize);
                if (!cells.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>();
                    cells.Add(key, indices);
                }
                indices.Add(i);
            }

            for (int i = 0; i < bounds.Count; i++)
                visibility[i] = CalculateVisibility(i, bounds, radii, cells, cellSize);
            return visibility;
        }

        private static float CalculateVisibility(int index, IReadOnlyList<Bounds> bounds, float[] radii,
            Dictionary<Vector3Int, List<int>> cells, float cellSize)
        {
            Vector3 center = bounds[index].center;
            Vector3Int key = Vector3Int.FloorToInt(center / cellSize);
            float occlusion = 0f;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (!cells.TryGetValue(key + new Vector3Int(x, y, z), out List<int> neighbors)) continue;
                foreach (int neighbor in neighbors)
                {
                    if (neighbor == index || radii[neighbor] == 0f) continue;
                    float combinedRadius = radii[index] + radii[neighbor];
                    float distanceSquared = (center - bounds[neighbor].center).sqrMagnitude;
                    float reach = 3f * combinedRadius;
                    if (distanceSquared >= reach * reach) continue;

                    float gap = Mathf.Max(0f, Mathf.Sqrt(distanceSquared) - combinedRadius);
                    float falloff = 1f - gap / (2f * combinedRadius);
                    float neighborRadiusSquared = radii[neighbor] * radii[neighbor];
                    // Approximate angular coverage, softened to zero at the finite search radius.
                    occlusion += neighborRadiusSquared / Mathf.Max(distanceSquared, neighborRadiusSquared) *
                        falloff * falloff;
                    if (occlusion >= MAX_OCCLUSION) return MIN_VISIBILITY;
                }
            }
            return 1f / (1f + occlusion);
        }
    }
}
