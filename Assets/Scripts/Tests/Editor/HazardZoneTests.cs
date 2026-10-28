using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class HazardZoneTests
    {
        private const float CELL_SIZE = 10f;
        private SpaceHazardData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<SpaceHazardData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        private HazardShape CreateEllipse() => TestHazards.CreateEllipse(_data, Vector3.zero, Vector3.right, 100f, 20f);

        [Test]
        public void Ellipse_FollowsOutlineAndIgnoresAltitude()
        {
            HazardShape shape = CreateEllipse();
            Assert.That(shape.Contains(new Vector3(0f, -900f, 19f), 0f, false), Is.True);
            Assert.That(shape.Contains(new Vector3(0f, 900f, 21f), 0f, false), Is.False);
            Assert.That(shape.Contains(new Vector3(90f, 0f, 7f), 0f, false), Is.True);
            Assert.That(shape.Contains(new Vector3(90f, 0f, 10f), 0f, false), Is.False);
            Assert.That(shape.Contains(new Vector3(101f, 0f, 0f), 0f, false), Is.False);
        }

        [Test]
        public void ExitHysteresis_OnlyExpandsExitBoundary()
        {
            HazardShape shape = CreateEllipse();
            Assert.That(shape.Contains(new Vector3(0f, 0f, 25f), 0f, false), Is.False);
            Assert.That(shape.Contains(new Vector3(0f, 0f, 25f), 0f, true), Is.True);
            Assert.That(shape.Contains(new Vector3(0f, 0f, 31f), 0f, true), Is.False);
        }

        [Test]
        public void Padding_IsDistanceToTheOutline()
        {
            HazardShape shape = CreateEllipse();
            Assert.That(shape.Contains(new Vector3(0f, 0f, 29.5f), 10f, false), Is.True);
            Assert.That(shape.Contains(new Vector3(0f, 0f, 30.5f), 10f, false), Is.False);
            Assert.That(shape.Contains(new Vector3(-300f, 0f, 0f), 150f, false), Is.False);
        }

        [Test]
        public void Outline_IsTheZeroDistanceAndInsideBounds()
        {
            HazardShape shape = CreateEllipse();
            for (int i = 0; i < 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle) * 100f, 0f, Mathf.Sin(angle) * 20f);
                Assert.That(shape.SignedDistance(point), Is.EqualTo(0f).Within(0.5f), $"angle {angle}");
                Assert.That(shape.Bounds.Contains(point), Is.True);
            }
            Assert.That(shape.SignedDistance(Vector3.zero), Is.EqualTo(-20f).Within(0.5f));
        }

        [Test]
        public void GeneratedClouds_AreOnePieceWithinTheirConservativeReach()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                HazardSpot stream = HazardCloudGenerator.CreateStream(_data, Vector3.zero, Vector3.right,
                    1800f, 220f, CELL_SIZE, new Random(seed));
                HazardSpot island = HazardCloudGenerator.CreateIsland(_data, Vector3.zero, Vector3.forward,
                    1200f, 300f, CELL_SIZE, new Random(seed));
                foreach (HazardSpot spot in new[] { stream, island })
                {
                    HazardShape shape = new HazardShape(spot);
                    Assert.That(CountInsidePieces(shape), Is.EqualTo(1), $"seed {seed}");
                    Bounds reach = spot.GetReach();
                    Random random = new Random(seed + 100);
                    for (int i = 0; i < 400; i++)
                    {
                        Vector3 point = new Vector3(Mathf.Lerp(reach.min.x - 200f, reach.max.x + 200f, (float)random.NextDouble()),
                            0f, Mathf.Lerp(reach.min.z - 200f, reach.max.z + 200f, (float)random.NextDouble()));
                        // Placement relies on the lobe proxy never claiming more room than the baked cloud leaves.
                        Assert.That(spot.GetClearance(point),
                            Is.LessThanOrEqualTo(Mathf.Max(0f, shape.SignedDistance(point)) + CELL_SIZE),
                            $"seed {seed}");
                    }
                }
            }
        }

        [Test]
        public void Island_IsMirrorSymmetricAcrossItsCenterLine()
        {
            for (int seed = 0; seed < 6; seed++)
            {
                Vector3 center = new Vector3(700f, 0f, -300f);
                Vector3 along = new Vector3(1f, 0f, 1f).normalized;
                HazardShape shape = new HazardShape(HazardCloudGenerator.CreateIsland(_data, center, along,
                    1200f, 300f, CELL_SIZE, new Random(seed)));
                Random random = new Random(seed);
                for (int i = 0; i < 300; i++)
                {
                    Vector3 point = new Vector3(Mathf.Lerp(shape.Bounds.min.x, shape.Bounds.max.x, (float)random.NextDouble()),
                        0f, Mathf.Lerp(shape.Bounds.min.z, shape.Bounds.max.z, (float)random.NextDouble()));
                    Vector3 offset = point - center;
                    Vector3 reflected = point - along * (2f * Vector3.Dot(offset, along));
                    Assert.That(shape.SignedDistance(reflected), Is.EqualTo(shape.SignedDistance(point)).Within(CELL_SIZE),
                        $"seed {seed}");
                }
            }
        }

        [Test]
        public void MirroredPair_HasPointSymmetricDistances()
        {
            HazardSpot spot = HazardCloudGenerator.CreateStream(_data, new Vector3(-900f, 0f, 400f),
                new Vector3(0.6f, 0f, 0.8f), 1800f, 220f, CELL_SIZE, new Random(3));
            HazardShape shape = new HazardShape(spot);
            HazardShape mirror = new HazardShape(spot.Mirror(Vector3.zero));
            Random random = new Random(3);
            for (int i = 0; i < 300; i++)
            {
                Vector3 point = new Vector3(Mathf.Lerp(shape.Bounds.min.x, shape.Bounds.max.x, (float)random.NextDouble()),
                    0f, Mathf.Lerp(shape.Bounds.min.z, shape.Bounds.max.z, (float)random.NextDouble()));
                Assert.That(mirror.SignedDistance(-point), Is.EqualTo(shape.SignedDistance(point)).Within(CELL_SIZE));
            }
        }

        private static int CountInsidePieces(HazardShape shape)
        {
            bool[] seen = new bool[shape.Columns * shape.Rows];
            Queue<int> queue = new Queue<int>();
            int pieces = 0;
            for (int start = 0; start < seen.Length; start++)
            {
                if (seen[start] || shape.GetDistance(start % shape.Columns, start / shape.Columns) > 0f) continue;
                pieces++;
                seen[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int column = index % shape.Columns;
                    int row = index / shape.Columns;
                    Visit(shape, seen, queue, column - 1, row);
                    Visit(shape, seen, queue, column + 1, row);
                    Visit(shape, seen, queue, column, row - 1);
                    Visit(shape, seen, queue, column, row + 1);
                }
            }
            return pieces;
        }

        private static void Visit(HazardShape shape, bool[] seen, Queue<int> queue, int column, int row)
        {
            if (column < 0 || row < 0 || column >= shape.Columns || row >= shape.Rows) return;
            int index = row * shape.Columns + column;
            if (seen[index] || shape.GetDistance(column, row) > 0f) return;
            seen[index] = true;
            queue.Enqueue(index);
        }
    }
}
