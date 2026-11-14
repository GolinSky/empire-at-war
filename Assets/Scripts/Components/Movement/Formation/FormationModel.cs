using System.Collections.Generic;
using System.Buffers;
using System;

namespace EmpireAtWar.Components.Movement.Formation
{
    public readonly struct FormationPoint
    {
        public float X { get; }
        public float Z { get; }

        public FormationPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
    }

    public static class FormationModel
    {
        private const int HEXAGONAL_RING_SIDE_COUNT = 6;

        public static FormationPoint CalculateCenter(IReadOnlyList<FormationPoint> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return default;
            }

            float x = 0f;
            float z = 0f;
            for (int i = 0; i < positions.Count; i++)
            {
                x += positions[i].X;
                z += positions[i].Z;
            }

            return new FormationPoint(x / positions.Count, z / positions.Count);
        }

        public static FormationPoint CalculateDestination(
            FormationPoint position,
            FormationPoint center,
            FormationPoint targetCenter)
        {
            return new FormationPoint(
                targetCenter.X + position.X - center.X,
                targetCenter.Z + position.Z - center.Z);
        }

        public static bool HasClearance(
            FormationPoint first,
            float firstRadius,
            FormationPoint second,
            float secondRadius)
        {
            if (firstRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(firstRadius));
            }

            if (secondRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(secondRadius));
            }

            float x = first.X - second.X;
            float z = first.Z - second.Z;
            float requiredDistance = firstRadius + secondRadius;
            return x * x + z * z >= requiredDistance * requiredDistance;
        }

        public static void CalculateDestinations(
            IReadOnlyList<FormationPoint> positions,
            FormationPoint targetCenter,
            IList<FormationPoint> destinations)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (destinations == null)
            {
                throw new ArgumentNullException(nameof(destinations));
            }

            destinations.Clear();
            FormationPoint center = CalculateCenter(positions);
            for (int i = 0; i < positions.Count; i++)
            {
                destinations.Add(CalculateDestination(
                    positions[i],
                    center,
                    targetCenter));
            }
        }

        public static void CalculateCompactDestinations(
            IReadOnlyList<FormationPoint> positions,
            IReadOnlyList<float> radii,
            FormationPoint targetCenter,
            IList<FormationPoint> destinations,
            float occupiedRadius = 0f) =>
            CalculateCompactDestinations(positions, radii, targetCenter, destinations, occupiedRadius,
                new List<FormationPoint>());

        internal static void CalculateCompactDestinations(
            IReadOnlyList<FormationPoint> positions,
            IReadOnlyList<float> radii,
            FormationPoint targetCenter,
            IList<FormationPoint> destinations,
            float occupiedRadius,
            List<FormationPoint> slots)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (radii == null)
            {
                throw new ArgumentNullException(nameof(radii));
            }

            if (destinations == null)
            {
                throw new ArgumentNullException(nameof(destinations));
            }

            if (positions.Count != radii.Count)
            {
                throw new ArgumentException(
                    "Formation positions and radii must have the same count.");
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
                    throw new ArgumentOutOfRangeException(
                        nameof(radii),
                        "Formation radii must be greater than zero.");
                }

                maximumRadius = Math.Max(maximumRadius, radii[i]);
                destinations.Add(targetCenter);
            }

            int assignedCount = occupiedRadius <= 0f ? 1 : 0;
            FormationPoint center = CalculateCenter(positions);
            double heading = Math.Atan2(targetCenter.Z - center.Z, targetCenter.X - center.X);
            BuildCompactSlots(slots, targetCenter,
                Math.Max(maximumRadius * 2f, occupiedRadius + maximumRadius + 0.1f),
                positions.Count - assignedCount, heading);
            int slotOffset = positions.Count;
            bool[] assigned = ArrayPool<bool>.Shared.Rent(slotOffset + slots.Count);
            try
            {
                Array.Clear(assigned, 0, slotOffset + slots.Count);
                if (assignedCount != 0)
                    assigned[FindClosestPosition(positions, targetCenter, assigned, 0)] = true;

                for (int assignmentIndex = assignedCount; assignmentIndex < positions.Count; assignmentIndex++)
                {
                    int positionIndex = FindClosestPosition(positions, targetCenter, assigned, 0);
                    int slotIndex = FindClosestPosition(slots, positions[positionIndex], assigned, slotOffset);
                    destinations[positionIndex] = slots[slotIndex];
                    assigned[positionIndex] = true;
                    assigned[slotOffset + slotIndex] = true;
                }
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(assigned);
            }
        }

        public static FormationPoint CalculateGridDestination(
            int index,
            int count,
            FormationPoint targetCenter,
            float spacing)
        {
            if (index < 0 || index >= count || count <= 0)
            {
                return targetCenter;
            }

            int columns = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (count + columns - 1) / columns;
            int row = index / columns;
            int rowStart = row * columns;
            int itemsInRow = Math.Min(columns, count - rowStart);
            int column = index - rowStart;

            float xOffset = (column - (itemsInRow - 1) * 0.5f) * spacing;
            float zOffset = (row - (rows - 1) * 0.5f) * spacing;
            return new FormationPoint(
                targetCenter.X + xOffset,
                targetCenter.Z + zOffset);
        }

        private static void BuildCompactSlots(
            List<FormationPoint> slots,
            FormationPoint center,
            float spacing,
            int requiredCount,
            double heading)
        {
            slots.Clear();
            for (int ring = 1; slots.Count < requiredCount; ring++)
            {
                int slotCount = HEXAGONAL_RING_SIDE_COUNT * ring;
                float radius = spacing * ring;
                for (int slot = 0; slot < slotCount; slot++)
                {
                    double angle = heading + slot * Math.PI * 2d / slotCount;
                    slots.Add(new FormationPoint(
                        center.X + (float)Math.Cos(angle) * radius,
                        center.Z + (float)Math.Sin(angle) * radius));
                }
            }
        }

        private static int FindClosestPosition(
            IReadOnlyList<FormationPoint> positions,
            FormationPoint target,
            bool[] excludedPositions,
            int offset)
        {
            int closestIndex = -1;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < positions.Count; i++)
            {
                if (excludedPositions[offset + i])
                {
                    continue;
                }

                float x = positions[i].X - target.X;
                float z = positions[i].Z - target.Z;
                float distance = x * x + z * z;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            if (closestIndex < 0)
            {
                throw new InvalidOperationException(
                    "No available formation position was found.");
            }

            return closestIndex;
        }
    }
}
