using System.Collections.Generic;
using System;

namespace EmpireAtWar.Components.Movement.Formation
{
    public readonly struct FormationPoint
    {
        public FormationPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }
        public float Z { get; }
    }

    public static class FormationModel
    {
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
    }
}
