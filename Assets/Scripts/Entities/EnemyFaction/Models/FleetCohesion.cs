using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Finds the main body of a task force so the AI commits ships together instead of one at a time.
    /// The anchor is the ship with the most fleet-mates around it; ships near the anchor form the body.
    /// </summary>
    public static class FleetCohesion
    {
        /// <summary>Ships farther than this from the anchor are stragglers that must rally before they are committed.</summary>
        public const float RADIUS = 800f;

        /// <summary>A body member leaves the body only this much farther out than a straggler joins it.</summary>
        public const float LEAVE_RADIUS_SCALE = 1.5f;

        /// <summary>Index of the position with the most others within <paramref name="radius"/>; ties keep the earlier one.</summary>
        public static int FindAnchor(IReadOnlyList<FormationPoint> positions, float radius)
        {
            float radiusSquared = radius * radius;
            int anchor = 0;
            int bestNeighbours = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                int neighbours = 0;
                for (int j = 0; j < positions.Count; j++)
                    if (i != j && DistanceSquared(positions[i], positions[j]) <= radiusSquared) neighbours++;
                if (neighbours <= bestNeighbours) continue;
                bestNeighbours = neighbours;
                anchor = i;
            }

            return anchor;
        }

        /// <summary>Neighbours the best anchor must add over the held one before the anchor changes.</summary>
        public const int ANCHOR_SWITCH_MARGIN = 2;

        /// <summary>
        /// Like <see cref="FindAnchor(IReadOnlyList{FormationPoint}, float)"/> but keeps <paramref name="heldIndex"/>
        /// unless another ship has clearly more fleet-mates, so orders anchored to it are not re-issued every decision.
        /// </summary>
        public static int FindAnchor(IReadOnlyList<FormationPoint> positions, float radius, int heldIndex)
        {
            int best = FindAnchor(positions, radius);
            if (heldIndex < 0 || heldIndex >= positions.Count || heldIndex == best) return best;
            return CountNeighbours(positions, best, radius) >=
                   CountNeighbours(positions, heldIndex, radius) + ANCHOR_SWITCH_MARGIN ? best : heldIndex;
        }

        private static int CountNeighbours(IReadOnlyList<FormationPoint> positions, int index, float radius)
        {
            float radiusSquared = radius * radius;
            int neighbours = 0;
            for (int j = 0; j < positions.Count; j++)
                if (index != j && DistanceSquared(positions[index], positions[j]) <= radiusSquared) neighbours++;
            return neighbours;
        }

        /// <summary>True when every position is within <paramref name="radius"/> of the anchor; an empty fleet is together.</summary>
        public static bool IsTogether(IReadOnlyList<FormationPoint> positions, float radius)
        {
            if (positions.Count == 0) return true;
            FormationPoint anchor = positions[FindAnchor(positions, radius)];
            foreach (FormationPoint position in positions)
                if (!IsInBody(anchor, position, radius, false)) return false;
            return true;
        }

        /// <summary>Whether a ship belongs to the body around <paramref name="anchor"/>; members get a wider margin to stay.</summary>
        public static bool IsInBody(FormationPoint anchor, FormationPoint ship, float radius, bool wasInBody)
        {
            float limit = wasInBody ? radius * LEAVE_RADIUS_SCALE : radius;
            return DistanceSquared(anchor, ship) <= limit * limit;
        }

        private static float DistanceSquared(FormationPoint a, FormationPoint b)
        {
            float x = a.X - b.X;
            float z = a.Z - b.Z;
            return x * x + z * z;
        }
    }
}
