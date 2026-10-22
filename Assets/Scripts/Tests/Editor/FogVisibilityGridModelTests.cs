using EmpireAtWar.Models.FogOfWar;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FogVisibilityGridModelTests
    {
        [Test]
        public void RevealDistance_MatchesDoubleSqrtForEveryExactlyRepresentableInteger()
        {
            using NativeArray<int> mismatch = new NativeArray<int>(1, Allocator.TempJob);
            new RevealDistanceParityJob { Mismatch = mismatch }.Schedule().Complete();
            Assert.That(mismatch[0], Is.EqualTo(-1), $"sqrt differs at {mismatch[0]}");

            foreach (int squared in new[] { 16777217, 16785409, 100000001, int.MaxValue })
                Assert.That(math.asint(FogRevealJob.Distance(squared)),
                    Is.EqualTo(math.asint((float)math.sqrt((double)squared))));
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(0f, true)]
        public void RevealRows_MatchLegacyCirclePixels(float softness, bool overflowingCircle = false)
        {
            const int RESOLUTION = 32;
            using NativeArray<float> target = new NativeArray<float>(RESOLUTION * RESOLUTION, Allocator.TempJob);
            using NativeArray<FogRevealCircle> circles = new NativeArray<FogRevealCircle>(overflowingCircle
                ? new[] { new FogRevealCircle { CenterX = -65536, CenterY = 0, Radius = 65536 } }
                : new[]
            {
                new FogRevealCircle { CenterX = 5, CenterY = 6, Radius = 4 },
                new FogRevealCircle { CenterX = 9, CenterY = 8, Radius = 6 },
                new FogRevealCircle { CenterX = 31, CenterY = 0, Radius = 3 },
                new FogRevealCircle { CenterX = -5, CenterY = 8, Radius = 4 },
                new FogRevealCircle { CenterX = 40, CenterY = 32, Radius = 8 },
                new FogRevealCircle { CenterX = 16, CenterY = 16, Radius = 1 }
            }, Allocator.TempJob);
            using NativeArray<float> expected = new NativeArray<float>(target.Length, Allocator.TempJob);
            float[] initial = new float[target.Length];
            for (int i = 0; i < target.Length; i++) initial[i] = (i % 7) * 0.03f;
            target.CopyFrom(initial);
            expected.CopyFrom(initial);
            new LegacyFogRevealJob
            {
                Target = expected, Circles = circles, Resolution = RESOLUTION,
                EdgeSoftness = softness, Intensity = 0.8f
            }.Schedule(RESOLUTION, 16).Complete();

            new FogRevealJob
            {
                Target = target, Circles = circles, Resolution = RESOLUTION,
                EdgeSoftness = softness, Intensity = 0.8f
            }.Schedule(RESOLUTION, 16).Complete();

            for (int i = 0; i < target.Length; i++)
                Assert.That(math.asint(target[i]), Is.EqualTo(math.asint(expected[i])), $"cell {i}");
        }

        [TestCase(0f)]
        [TestCase(0.01f)]
        [TestCase(0.1f)]
        [TestCase(1f)]
        public void FadeAndWrite_MatchesLegacyFadeAndByteConversion(float maxDelta)
        {
            const int COUNT = 4096;
            using NativeArray<float> current = new NativeArray<float>(COUNT, Allocator.TempJob);
            using NativeArray<float> target = new NativeArray<float>(COUNT, Allocator.TempJob);
            using NativeArray<byte> pixels = new NativeArray<byte>(COUNT, Allocator.TempJob);
            using NativeArray<int> changed = new NativeArray<int>(1, Allocator.TempJob);
            float[] initial = new float[COUNT];
            float[] targets = new float[COUNT];
            float[] expected = new float[COUNT];
            System.Random random = new System.Random(471);
            for (int i = 0; i < COUNT; i++)
            {
                initial[i] = expected[i] = (float)random.NextDouble();
                targets[i] = i % 5 == 0 ? initial[i] : (float)random.NextDouble();
                float delta = targets[i] - expected[i];
                if (delta == 0f) continue;
                expected[i] = math.abs(delta) <= maxDelta
                    ? targets[i]
                    : expected[i] + (delta > 0f ? maxDelta : -maxDelta);
            }
            current.CopyFrom(initial);
            target.CopyFrom(targets);

            new FogFadeWriteJob
            {
                Current = current, Target = target, Pixels = pixels, Changed = changed, MaxDelta = maxDelta
            }.Schedule().Complete();

            Assert.That(changed[0], Is.EqualTo(1));
            for (int i = 0; i < COUNT; i++)
            {
                Assert.That(math.asint(current[i]), Is.EqualTo(math.asint(expected[i])), $"cell {i}");
                Assert.That(pixels[i], Is.EqualTo((byte)(expected[i] * 255f + 0.5f)), $"pixel {i}");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScheduledChain_MatchesSeparateOperationsAcrossRebuildAndFadeFrames(bool keepHistory)
        {
            using FogVisibilityGridModel separate = new FogVisibilityGridModel(32);
            using FogVisibilityGridModel chained = new FogVisibilityGridModel(32);
            using NativeArray<byte> expected = new NativeArray<byte>(32 * 32, Allocator.TempJob);
            using NativeArray<byte> actual = new NativeArray<byte>(32 * 32, Allocator.TempJob);
            for (int frame = 0; frame < 20; frame++)
            {
                bool rebuild = frame % 3 == 0;
                if (rebuild)
                {
                    separate.ResetTargets(keepHistory);
                    // The final rebuild has no vision sources.
                    if (frame < 18)
                    {
                        separate.QueueReveal(frame, 12, 5);
                        chained.QueueReveal(frame, 12, 5);
                    }
                    separate.ApplyReveals(0.25f, 1f);
                }

                bool expectedChanged = separate.Fade(0.1f);
                separate.WriteBytes(expected);
                bool actualChanged = chained.UpdateAndWriteBytes(0.1f, actual, rebuild, keepHistory, 0.25f, 1f);
                Assert.That(actualChanged, Is.EqualTo(expectedChanged), $"frame {frame}");
                for (int i = 0; i < actual.Length; i++)
                {
                    Assert.That(math.asint(chained.GetVisibility(i)),
                        Is.EqualTo(math.asint(separate.GetVisibility(i))), $"frame {frame}, cell {i}");
                    Assert.That(actual[i], Is.EqualTo(expected[i]), $"frame {frame}, pixel {i}");
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct LegacyFogRevealJob : IJobParallelFor
        {
            // Same invariant as production: each iteration owns its full row.
            [NativeDisableParallelForRestriction] public NativeArray<float> Target;
            [ReadOnly] public NativeArray<FogRevealCircle> Circles;
            public int Resolution;
            public float EdgeSoftness;
            public float Intensity;

            public void Execute(int y)
            {
                int rowOffset = y * Resolution;
                for (int i = 0; i < Circles.Length; i++)
                {
                    FogRevealCircle circle = Circles[i];
                    int outerRadius = (int)math.ceil(circle.Radius * (1f + EdgeSoftness));
                    int dy = y - circle.CenterY;
                    if (dy < -outerRadius || dy > outerRadius) continue;

                    int minX = math.clamp(circle.CenterX - outerRadius, 0, Resolution - 1);
                    int maxX = math.clamp(circle.CenterX + outerRadius, 0, Resolution - 1);
                    int sqrOuterRadius = outerRadius * outerRadius;
                    for (int x = minX; x <= maxX; x++)
                    {
                        int dx = x - circle.CenterX;
                        int distSqr = dx * dx + dy * dy;
                        if (distSqr > sqrOuterRadius) continue;

                        float distance = (float)math.sqrt((double)distSqr);
                        float visibility =
                            FogVisibilityModel.SoftVisibility(distance, circle.Radius, EdgeSoftness) * Intensity;
                        int index = rowOffset + x;
                        if (visibility > Target[index]) Target[index] = visibility;
                    }
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct RevealDistanceParityJob : IJob
        {
            [WriteOnly] public NativeArray<int> Mismatch;

            public void Execute()
            {
                int mismatch = -1;
                for (int squared = 0; squared <= 1 << 24; squared++)
                {
                    float expected = (float)math.sqrt((double)squared);
                    if (math.asint(FogRevealJob.Distance(squared)) != math.asint(expected))
                    {
                        mismatch = squared;
                        break;
                    }
                }
                Mismatch[0] = mismatch;
            }
        }

        [Test]
        public void Fade_MovesTowardRevealedTargetByMaxDelta()
        {
            using FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.ResetTargets(false);
            grid.Reveal(4, 4, 2, 0f, 1f);

            Assert.That(grid.Fade(0.25f), Is.True);
            Assert.That(grid.GetVisibility(4, 4), Is.EqualTo(0.25f));
            Assert.That(grid.GetVisibility(0, 0), Is.EqualTo(0f));
        }

        [Test]
        public void ResetTargets_WithHistory_KeepsExploredCellsDimlyVisible()
        {
            using FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.Reveal(4, 4, 2, 0f, 1f);
            grid.Fade(1f);

            grid.ResetTargets(true);
            grid.Fade(1f);

            Assert.That(grid.GetVisibility(4, 4), Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(grid.GetVisibility(0, 0), Is.EqualTo(0f));
        }

        [Test]
        public void ResetTargets_WithoutHistory_ReturnsFullFog()
        {
            using FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.Reveal(4, 4, 2, 0f, 1f);
            grid.Fade(1f);

            grid.ResetTargets(false);
            grid.Fade(1f);

            Assert.That(grid.GetVisibility(4, 4), Is.EqualTo(0f));
            Assert.That(grid.Fade(1f), Is.False);
        }

        [Test]
        public void QueuedReveals_MatchSeparateReveals()
        {
            using FogVisibilityGridModel separate = new FogVisibilityGridModel(32);
            using FogVisibilityGridModel queued = new FogVisibilityGridModel(32);
            separate.Reveal(5, 6, 4, 0.5f, 1f);
            separate.Reveal(9, 8, 6, 0.5f, 1f);
            separate.Reveal(31, 0, 3, 0.5f, 1f);
            queued.QueueReveal(5, 6, 4);
            queued.QueueReveal(9, 8, 6);
            queued.QueueReveal(31, 0, 3);
            queued.ApplyReveals(0.5f, 1f);
            separate.Fade(1f);
            queued.Fade(1f);

            for (int i = 0; i < 32 * 32; i++)
                Assert.That(queued.GetVisibility(i), Is.EqualTo(separate.GetVisibility(i)), $"cell {i}");
            Assert.That(queued.GetVisibility(9, 8), Is.EqualTo(1f));
        }

        [Test]
        public void GetVisibility_OutsideGrid_SamplesClampedEdge()
        {
            using FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.Reveal(7, 0, 1, 0f, 1f);
            grid.Fade(1f);

            Assert.That(grid.GetVisibility(20, -5), Is.EqualTo(grid.GetVisibility(7, 0)));
            Assert.That(grid.GetVisibility(7, 0), Is.EqualTo(1f));
        }
    }
}
