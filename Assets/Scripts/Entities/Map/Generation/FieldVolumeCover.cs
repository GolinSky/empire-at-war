using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Covers a field with a few large overlapping circles, largest inscribed circle first,
    /// so navigation sees one solid volume instead of individual rocks.
    /// </summary>
    public static class FieldVolumeCover
    {
        // A circle this share of a cell wide still covers the corners of a one-cell sliver.
        private const float MINIMUM_RADIUS_SHARE = 0.75f;
        private const float PARTIAL_COVER_SHARE = 0.5f;

        public static List<FieldVolume> Cover(FieldGrid grid, IReadOnlyList<int> cells, int[] edgeDistances)
        {
            List<FieldVolume> volumes = new List<FieldVolume>();
            List<int> uncovered = new List<int>(cells);
            while (uncovered.Count > 0)
            {
                int deepest = uncovered[0];
                foreach (int cell in uncovered)
                {
                    if (edgeDistances[cell] > edgeDistances[deepest])
                    {
                        deepest = cell;
                    }
                }

                // Edge distance counts steps to the first open cell; the open border lies half a cell nearer.
                float radius = Mathf.Max(
                    grid.CellSize * MINIMUM_RADIUS_SHARE,
                    (edgeDistances[deepest] - 0.5f) * grid.CellSize);
                Vector3 center = grid.GetCenter(deepest);
                volumes.Add(new FieldVolume(center, radius));
                // Cells the circle only partly reaches count as covered; the next circle would add little.
                float coverage = radius + grid.CellSize * PARTIAL_COVER_SHARE;
                uncovered.RemoveAll(cell => MapGeometry.Distance(grid.GetCenter(cell), center) <= coverage);
            }

            return volumes;
        }
    }
}
