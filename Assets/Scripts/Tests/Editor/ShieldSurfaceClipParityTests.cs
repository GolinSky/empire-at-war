using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace EmpireAtWar.Tests.Health
{
    public sealed class ShieldSurfaceClipParityTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(31)]
        [TestCase(1024)]
        public void PackedJob_MatchesOriginalForRandomSegmentsAndPartialGroups(int planeCount)
        {
            var random = new Random(71237u);
            var planes = new Vector4[planeCount];
            for (int i = 0; i < planeCount; i++)
            {
                float3 normal = math.normalize(random.NextFloat3(-1f, 1f));
                planes[i] = new Vector4(normal.x, normal.y, normal.z, random.NextFloat(1f, 10f));
            }
            var origins = new float3[128];
            var directions = new float3[origins.Length];
            for (int i = 0; i < origins.Length; i++)
            {
                origins[i] = random.NextFloat3(-20f, 20f);
                directions[i] = random.NextFloat3(-5f, 5f) - origins[i];
            }
            AssertParity(planes, origins, directions);
        }

        [Test]
        public void PackedJob_MatchesOriginalForParallelInsideMissTangentAndEndpoints()
        {
            Vector4[] planes =
            {
                new Vector4(1f, 0f, 0f, 1f), new Vector4(-1f, 0f, 0f, 1f),
                new Vector4(0f, 1f, 0f, 1f), new Vector4(0f, -1f, 0f, 1f),
                new Vector4(0f, 0f, 1f, 1f), new Vector4(0f, 0f, -1f, 1f)
            };
            float3[] origins =
            {
                new float3(3f, 0f, 0f), float3.zero, new float3(2f, 0f, 0f),
                new float3(2f, 1f, 0f), new float3(1f, 0f, 0f), new float3(2f, 0f, 0f),
                new float3(2f, 2f, 0f), new float3(0f, 2f, 0f), float3.zero
            };
            float3[] directions =
            {
                new float3(-4f, 0f, 0f), new float3(2f, 0f, 0f), new float3(0f, 2f, 0f),
                new float3(-4f, 0f, 0f), new float3(-1f, 0f, 0f), new float3(-1f, 0f, 0f),
                new float3(-1f, -1f, 0f), new float3(1f, 0f, 0f), float3.zero
            };
            AssertParity(planes, origins, directions);
        }

        [Test]
        public void PackedJob_PreservesSignedZeroTieOrderAcrossGroups()
        {
            float negativeZero = math.asfloat(unchecked((int)0x80000000));
            for (int count = 1; count <= 9; count++)
            {
                var planes = new Vector4[count];
                for (int i = 0; i < count; i++)
                    planes[i] = new Vector4(1f, 0f, 0f, i % 2 == 0 ? negativeZero : 0f);
                AssertParity(planes, new[] { float3.zero }, new[] { new float3(-1f, 0f, 0f) });
            }
        }

        [Test]
        public void PackedJob_PreservesNonFiniteComparisonAndParallelMissBehavior()
        {
            float[] values = { 0f, -1f, 1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue };
            foreach (float value in values)
            {
                Vector4[] planes =
                {
                    new Vector4(1f, 0f, 0f, 1f), new Vector4(value, 1f, 0f, -1f),
                    new Vector4(-1f, 0f, 0f, 1f), new Vector4(0f, 0f, 1f, value)
                };
                AssertParity(planes, new[] { new float3(2f, 0f, 0f), new float3(value, 1f, 1f) },
                    new[] { new float3(-3f, value, 0f), new float3(-3f, 0f, 0f) });
            }
        }

        [Test]
        public void PackPlanes_PreservesColumnsAndPadsWithNonRejectingPlanes()
        {
            Vector4[] source = { new Vector4(1f, 2f, 3f, 4f), new Vector4(5f, 6f, 7f, 8f), new Vector4(9f, 10f, 11f, 12f) };
            using var groups = Shield.PackPlanes(source, Allocator.TempJob);
            Assert.That(groups.Length, Is.EqualTo(1));
            for (int i = 0; i < source.Length; i++)
            {
                Assert.That(groups[0].c0[i], Is.EqualTo(source[i].x));
                Assert.That(groups[0].c1[i], Is.EqualTo(source[i].y));
                Assert.That(groups[0].c2[i], Is.EqualTo(source[i].z));
                Assert.That(groups[0].c3[i], Is.EqualTo(source[i].w));
            }
            Assert.That(groups[0].c0.w, Is.Zero);
            Assert.That(groups[0].c1.w, Is.Zero);
            Assert.That(groups[0].c2.w, Is.Zero);
            Assert.That(groups[0].c3.w, Is.EqualTo(float.PositiveInfinity));
        }

        private static void AssertParity(Vector4[] source, float3[] origins, float3[] directions)
        {
            using var originalPlanes = new NativeArray<Vector4>(source, Allocator.TempJob);
            using var packedPlanes = Shield.PackPlanes(source, Allocator.TempJob);
            using var expected = new NativeArray<float>(1, Allocator.TempJob);
            using var actual = new NativeArray<float>(1, Allocator.TempJob);
            for (int i = 0; i < origins.Length; i++)
            {
                new OriginalJob
                {
                    Planes = originalPlanes.Reinterpret<float4>(), Origin = origins[i], Direction = directions[i], Enter = expected
                }.Schedule().Complete();
                new ShieldSurfaceClipJob
                {
                    Planes = packedPlanes, Origin = origins[i], Direction = directions[i], Enter = actual
                }.Schedule().Complete();
                Assert.That(math.asint(actual[0]), Is.EqualTo(math.asint(expected[0])), $"Segment {i}, {source.Length} planes");
            }
        }

        // Frozen original kernel: independent reference with the same Burst float defaults.
        [BurstCompile]
        internal struct OriginalJob : IJob
        {
            public const float MISS = -1f;

            [ReadOnly] public NativeArray<float4> Planes;
            public float3 Origin;
            public float3 Direction;
            [WriteOnly] public NativeArray<float> Enter;

            public void Execute()
            {
                float enter = float.NegativeInfinity;
                float exit = float.PositiveInfinity;
                for (int i = 0; i < Planes.Length; i++)
                {
                    float3 normal = Planes[i].xyz;
                    float approach = math.dot(normal, Direction);
                    float gap = Planes[i].w - math.dot(normal, Origin);
                    if (approach < 0f) enter = math.max(enter, gap / approach);
                    else if (approach > 0f) exit = math.min(exit, gap / approach);
                    else if (gap < 0f)
                    {
                        Enter[0] = MISS;
                        return;
                    }
                }

                // An origin inside the shield must not shoot through the hull to its far surface.
                Enter[0] = enter > exit || enter < 0f || enter > 1f ? MISS : enter;
            }
        }

    }
}
