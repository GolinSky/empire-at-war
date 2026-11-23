using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Finds the main body of a task force so the AI commits ships together instead of one at a time.
    /// The anchor is the ship with the most fleet-mates around it; ships near the anchor form the body.
    /// Distances are hull gaps: centre distance minus both hull radii (W55), so a Viscount
    /// (NavigationRadius 895 &gt; <see cref="RADIUS"/>) still counts as beside its fleet.
    /// </summary>
    public static class FleetCohesion
    {
        /// <summary>Ships whose hull is farther than this from the anchor's hull are stragglers that must rally first.</summary>
        public const float RADIUS = 800f;

        /// <summary>A body member leaves the body only this much farther out than a straggler joins it.</summary>
        public const float LEAVE_RADIUS_SCALE = 1.5f;

        /// <summary>Index of the position with the most others within <paramref name="radius"/> hull gap; ties keep the earlier one.</summary>
        public static int FindAnchor(IReadOnlyList<FormationPoint> positions, IReadOnlyList<float> hullRadii,
            float radius)
        {
            int anchor = 0;
            int bestNeighbours = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                int neighbours = CountNeighbours(positions, hullRadii, i, radius);
                if (neighbours <= bestNeighbours) continue;
                bestNeighbours = neighbours;
                anchor = i;
            }

            return anchor;
        }

        /// <summary>Neighbours the best anchor must add over the held one before the anchor changes.</summary>
        public const int ANCHOR_SWITCH_MARGIN = 2;

        /// <summary>
        /// Like <see cref="FindAnchor(IReadOnlyList{FormationPoint}, IReadOnlyList{float}, float)"/> but keeps
        /// <paramref name="heldIndex"/> unless another ship has clearly more fleet-mates, so orders anchored to it are not
        /// re-issued every decision.
        /// </summary>
        public static int FindAnchor(IReadOnlyList<FormationPoint> positions, IReadOnlyList<float> hullRadii,
            float radius, int heldIndex)
        {
            int best = FindAnchor(positions, hullRadii, radius);
            if (heldIndex < 0 || heldIndex >= positions.Count || heldIndex == best) return best;
            return CountNeighbours(positions, hullRadii, best, radius) >=
                   CountNeighbours(positions, hullRadii, heldIndex, radius) + ANCHOR_SWITCH_MARGIN ? best : heldIndex;
        }

        private static int CountNeighbours(IReadOnlyList<FormationPoint> positions, IReadOnlyList<float> hullRadii,
            int index, float radius)
        {
            int neighbours = 0;
            for (int j = 0; j < positions.Count; j++)
                if (index != j && IsWithin(positions[index], hullRadii[index], positions[j], hullRadii[j], radius))
                    neighbours++;
            return neighbours;
        }

        /// <summary>True when every hull is within <paramref name="radius"/> of the anchor's hull; an empty fleet is together.</summary>
        public static bool IsTogether(IReadOnlyList<FormationPoint> positions, IReadOnlyList<float> hullRadii,
            float radius)
        {
            if (positions.Count == 0) return true;
            int anchor = FindAnchor(positions, hullRadii, radius);
            for (int i = 0; i < positions.Count; i++)
                if (!IsInBody(positions[anchor], hullRadii[anchor], positions[i], hullRadii[i], radius, false))
                    return false;
            return true;
        }

        /// <summary>Whether a ship belongs to the body around <paramref name="anchor"/>; members get a wider margin to stay.</summary>
        public static bool IsInBody(FormationPoint anchor, float anchorRadius, FormationPoint ship, float shipRadius,
            float radius, bool wasInBody) =>
            IsWithin(anchor, anchorRadius, ship, shipRadius, wasInBody ? radius * LEAVE_RADIUS_SCALE : radius);

        private static bool IsWithin(FormationPoint a, float aRadius, FormationPoint b, float bRadius, float gap)
        {
            float limit = gap + aRadius + bRadius;
            float x = a.X - b.X;
            float z = a.Z - b.Z;
            return x * x + z * z <= limit * limit;
        }
    }
}
