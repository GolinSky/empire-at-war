using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Candidate geometry follows each archetype's accessible approaches to the main relay.</summary>
    public static class StationSlotGeometry
    {
        private const int SLOT_VARIANTS = 16;
        private const float ORBITAL_EXCLUSION = 60f;
        private const float FLANK_HALF_SPAN = 0.7f;

        public static Vector2[] Create(MapSizeSettings size, int count, Vector2 available, Random random, int planetSeed)
        {
            Vector2[] offsets = size.Archetype == MapArchetypeKind.TwinPass
                ? CreateFlanks(size, count, random)
                : CreateRing(size, count, random, planetSeed);
            float scale = float.MaxValue;
            foreach (Vector2 offset in offsets)
            {
                if (Mathf.Abs(offset.x) > 0.001f) scale = Mathf.Min(scale, available.x / Mathf.Abs(offset.x));
                if (Mathf.Abs(offset.y) > 0.001f) scale = Mathf.Min(scale, available.y / Mathf.Abs(offset.y));
            }
            scale *= Mathf.Lerp(0.96f, 1f, (float)random.NextDouble());
            for (int i = 0; i < offsets.Length; i++) offsets[i] *= scale;
            return offsets;
        }

        private static Vector2[] CreateRing(MapSizeSettings size, int count, Random random, int planetSeed)
        {
            float step = Mathf.PI * 2f / count;
            float phase = (uint)planetSeed % SLOT_VARIANTS * Mathf.PI * 2f / SLOT_VARIANTS;
            if (size.Archetype == MapArchetypeKind.OrbitalRing)
            {
                phase = -size.SkeletonAngle * Mathf.Deg2Rad;
                if (count > 2)
                {
                    // Keep the near-main pole free: starts span the remaining arc, with fairness checked on lanes.
                    phase += (90f + ORBITAL_EXCLUSION) * Mathf.Deg2Rad;
                    step = (360f - 2f * ORBITAL_EXCLUSION) * Mathf.Deg2Rad / (count - 1);
                }
            }
            else phase += random.Next(3) * Mathf.PI / SLOT_VARIANTS;
            phase += ((float)random.NextDouble() - 0.5f) * size.StationPhaseJitterRadians;
            Vector2[] offsets = new Vector2[count];
            for (int i = 0; i < count; i++)
                offsets[i] = new Vector2(Mathf.Cos(phase + step * i), Mathf.Sin(phase + step * i));
            return offsets;
        }

        private static Vector2[] CreateFlanks(MapSizeSettings size, int count, Random random)
        {
            float angle = size.SkeletonAngle + ((float)random.NextDouble() - 0.5f) *
                size.StationPhaseJitterRadians * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector2[] offsets = new Vector2[count];
            int leftCount = (count + 1) / 2;
            for (int i = 0; i < count; i++)
            {
                bool left = i < leftCount;
                int sideCount = left ? leftCount : count - leftCount;
                int index = left ? i : i - leftCount;
                float along = sideCount == 1 ? (count == 3 ? -FLANK_HALF_SPAN : 0f) : Mathf.Lerp(-FLANK_HALF_SPAN, FLANK_HALF_SPAN,
                    index / (float)(sideCount - 1));
                // Walk up one side and down the other so sorted teammates retain contiguous sectors.
                float across = Mathf.Sqrt(1f + FLANK_HALF_SPAN * FLANK_HALF_SPAN - along * along);
                Vector3 offset = rotation * new Vector3(left ? -across : across, 0f, left ? along : -along);
                offsets[i] = new Vector2(offset.x, offset.z);
            }
            return offsets;
        }
    }
}
