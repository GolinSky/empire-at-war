using System.Collections.Generic;

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
    }
}
