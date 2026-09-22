using System;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Movement.Formation
{
    public static class BattleFormationModel
    {
        public static void CalculateDestinations(
            IReadOnlyList<FormationPoint> positions,
            IReadOnlyList<float> radii,
            FormationPoint objective,
            IList<FormationPoint> destinations)
        {
            if (positions.Count != radii.Count)
            {
                throw new ArgumentException("Formation positions and radii must have the same count.");
            }

            destinations.Clear();
            if (positions.Count == 0)
            {
                return;
            }

            float maximumRadius = 0f;
            for (int i = 0; i < radii.Count; i++)
            {
                if (radii[i] <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(radii));
                }

                maximumRadius = Math.Max(maximumRadius, radii[i]);
                destinations.Add(objective);
            }

            // A perimeter leaves the objective clear; chord spacing includes a small gap.
            float spacing = maximumRadius * 2.1f;
            float radius = Math.Max(maximumRadius * 3f,
                positions.Count > 1
                    ? spacing / (2f * (float)Math.Sin(Math.PI / positions.Count))
                    : spacing);
            // ShipService preserves fleet order, so reissued orders retain slot ownership.
            for (int i = 0; i < positions.Count; i++)
            {
                double angle = i * Math.PI * 2d / positions.Count;
                destinations[i] = new FormationPoint(
                    objective.X + (float)Math.Cos(angle) * radius,
                    objective.Z + (float)Math.Sin(angle) * radius);
            }
        }
    }
}
