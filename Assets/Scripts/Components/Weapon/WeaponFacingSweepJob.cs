using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    [BurstCompile]
    internal struct WeaponFacingSweepJob : IJob
    {
        [ReadOnly] public NativeArray<Vector2> Offsets;
        [ReadOnly] public NativeArray<float> Heights;
        [ReadOnly] public NativeArray<Vector2> Rotations;
        [ReadOnly] public NativeArray<int> Indices;
        [ReadOnly] public NativeArray<Mount> Mounts;
        public NativeArray<Vector2> Targets;
        [WriteOnly] public NativeArray<Bounds> Scores;
        public int MaximumScore;

        public void Execute()
        {
            for (int step = 0; step < Scores.Length; step++)
            {
                Vector2 rotation = Rotations[step];
                for (int i = 0; i < Offsets.Length; i++)
                {
                    Vector2 offset = Offsets[i];
                    Targets[i] = new Vector2(rotation.x * offset.x + rotation.y * offset.y,
                        -rotation.y * offset.x + rotation.x * offset.y);
                }

                Bounds bounds = default;
                foreach (Mount mount in Mounts)
                {
                    bool uncertain = false;
                    bool canFire = false;
                    for (int i = mount.Start; i < mount.End; i++)
                    {
                        int index = Indices[i];
                        Vector2 target = Targets[index];
                        float dx = target.x - mount.Offset.x;
                        float dz = target.y - mount.Offset.z;
                        float distanceSquared = dx * dx + dz * dz;
                        if (distanceSquared > mount.RangeSquared) continue;
                        if (mount.FullCircle)
                        {
                            canFire = true;
                            break;
                        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        bounds.AimChecks++;
#endif
                        int result = mount.Classify(dx, Heights[index] - mount.Offset.y, dz, distanceSquared);
                        if (result > 0)
                        {
                            canFire = true;
                            break;
                        }
                        uncertain |= result == 0;
                    }

                    if (canFire) bounds.Lower += mount.Weight;
                    if (canFire || uncertain) bounds.Upper += mount.Weight;
                }

                Scores[step] = bounds;
                if (bounds.Lower == MaximumScore) break;
            }
        }

        internal struct Bounds
        {
            public int Lower;
            public int Upper;
            public int AimChecks;
        }

        internal struct Mount
        {
            private const float EDGE_TOLERANCE_SQUARED = 0.00000305f; // sin²(0.1°)

            public Vector3 Offset;
            public Vector3 Right;
            public Vector3 Forward;
            public Vector2 MinEdge;
            public Vector2 MaxEdge;
            public float RangeSquared;
            public float InnerRangeSquared;
            public int Weight;
            public int Start;
            public int End;
            public bool FullCircle;
            public bool WideArc;
            public bool ValidArc;

            // Zero means the legacy aim check must decide this pair.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Classify(float dx, float dy, float dz, float distanceSquared)
            {
                float x = dx * Right.x + dy * Right.y + dz * Right.z;
                float z = dx * Forward.x + dy * Forward.y + dz * Forward.z;
                float lengthSquared = distanceSquared + dy * dy;
                float localLengthSquared = x * x + z * z;
                if (!ValidArc || distanceSquared <= 1e-10f || distanceSquared <= lengthSquared * 1e-8f ||
                    localLengthSquared <= lengthSquared * 1e-4f || distanceSquared >= InnerRangeSquared ||
                    !math.isfinite(lengthSquared))
                    return 0;

                float afterMin = x * MinEdge.y - z * MinEdge.x;
                float beforeMax = z * MaxEdge.x - x * MaxEdge.y;
                float threshold = localLengthSquared * EDGE_TOLERANCE_SQUARED;
                if (afterMin * afterMin <= threshold || beforeMax * beforeMax <= threshold) return 0;
                bool inside = WideArc ? afterMin > 0f || beforeMax > 0f : afterMin > 0f && beforeMax > 0f;
                return inside ? 1 : -1;
            }
        }
    }
}
