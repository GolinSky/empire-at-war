using System.Collections.Generic;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipSpacingJobTests
    {
        [Test]
        public void CoincidentEligibleShips_OnlyHigherRegistrationYieldsWithNonzeroGoal()
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, float2.zero) };
            ShipSpacingResult anchor = Solve(ships, 0);
            ShipSpacingResult yielding = Solve(ships, 1);

            Assert.That(anchor.HasOverlap, Is.True);
            Assert.That(anchor.HasCorrection, Is.False);
            Assert.That(yielding.HasCorrection, Is.True);
            Assert.That(yielding.BlockerId, Is.EqualTo(1));
            Assert.That(math.length(yielding.Goal), Is.GreaterThan(10f));
            Assert.That(math.length(yielding.Goal - Solve(ships, 1).Goal), Is.Zero);
        }

        [Test]
        public void IneligibleBlocker_HigherRegistrationStillAnchors()
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, new float2(2f, 0f)) };
            ships[1].Eligible = false;

            Assert.That(Solve(ships, 0).HasCorrection, Is.True);
            Assert.That(Solve(ships, 1).HasCorrection, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MovingShip_DoesNotBlockOrReceiveCorrection(bool eligible)
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, new float2(2f, 0f)) };
            ships[1].Moving = true;
            ships[1].Eligible = eligible;

            Assert.That(Solve(ships, 0).HasCorrection, Is.False);
            Assert.That(Solve(ships, 1).HasCorrection, Is.False);
            uint movingHash = Solve(ships, 0).OccupancyHash;
            ships[1].Moving = false;
            Assert.That(Solve(ships, 0).OccupancyHash, Is.Not.EqualTo(movingHash));
            Assert.That(Solve(ships, eligible ? 1 : 0).HasCorrection, Is.True);
        }

        [Test]
        public void FailedAnchoredBlocker_LowerRegistrationCanYield()
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, new float2(2f, 0f)) };
            ships[1].Anchored = true;

            Assert.That(Solve(ships, 0).HasCorrection, Is.True);
        }

        [Test]
        public void TouchingVerticalSpans_DoNotOverlap()
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, float2.zero) };
            ships[1].Bottom = ships[0].Top;
            ships[1].Top = ships[1].Bottom + 10f;

            Assert.That(Solve(ships, 1).HasOverlap, Is.False);
        }

        [Test]
        public void LargeAndSmallHulls_AcrossCellBoundaryAreDetected()
        {
            var ships = new[] { Ship(1, new float2(199f, 0f)), Ship(2, new float2(250f, 0f)) };
            ships[0].Radius = 100f;
            ships[1].Radius = 1f;

            ShipSpacingResult result = Solve(ships, 1);

            Assert.That(result.HasCorrection, Is.True);
            Assert.That(result.Penetration, Is.EqualTo(50f));
            Assert.That(math.distance(result.Goal, ships[0].Position), Is.GreaterThan(101f));
        }

        [Test]
        public void DenseCell_RejectedCandidatesConsumeBudgetAndCursorFindsLaterBlocker()
        {
            var ships = new ShipSpacingInput[ShipSpacingJob.MAX_VISITED * 2 + 1];
            ships[0] = Ship(1000, float2.zero);
            for (int i = 1; i < ships.Length; i++)
            {
                ships[i] = Ship(i, float2.zero);
                ships[i].Bottom = 20f;
                ships[i].Top = 30f;
            }
            ships[ships.Length - 1].Bottom = -5f;
            ships[ships.Length - 1].Top = 5f;

            bool found = false;
            for (int batch = 0; batch < 3; batch++)
            {
                ShipSpacingResult result = Solve(ships, 0);
                Assert.That(result.Visited, Is.EqualTo(ShipSpacingJob.MAX_VISITED));
                Assert.That(result.Saturated, Is.True);
                found |= result.HasCorrection;
                ships[0].Cursor = result.NextCursor;
            }
            Assert.That(found, Is.True);
        }

        [Test]
        public void EqualPenetrations_SelectStableLowestBlockerIdRegardlessOfSnapshotOrder()
        {
            var own = Ship(10, float2.zero);
            var left = Ship(1, new float2(-2f, 0f));
            var right = Ship(2, new float2(2f, 0f));

            Assert.That(Solve(new[] { own, left, right }, 0).BlockerId, Is.EqualTo(1));
            Assert.That(Solve(new[] { right, own, left }, 1).BlockerId, Is.EqualTo(1));
        }

        [Test]
        public void SaturatedQuery_HashesUnvisitedOccupancyIndependentlyOfCursor()
        {
            var ships = new ShipSpacingInput[ShipSpacingJob.MAX_VISITED * 2 + 1];
            for (int i = 0; i < ships.Length; i++) ships[i] = Ship(i, float2.zero);
            ShipSpacingResult first = Solve(ships, 0);
            ships[0].Cursor = first.NextCursor;
            ShipSpacingResult nextSlice = Solve(ships, 0);

            Assert.That(first.Saturated, Is.True);
            Assert.That(nextSlice.OccupancyHash, Is.EqualTo(first.OccupancyHash));
            ships[0].Cursor = 0;
            ships[ships.Length - 1].Position = new float2(0.2f, 0f);
            ShipSpacingResult changed = Solve(ships, 0);
            Assert.That(changed.OccupancyHash, Is.Not.EqualTo(first.OccupancyHash));
            Assert.That(changed.Visited, Is.EqualTo(ShipSpacingJob.MAX_VISITED));
        }

        [Test]
        public void ExactPlanarContact_IsClearAndPreservesSnapshotIdentity()
        {
            var ships = new[] { Ship(1, float2.zero), Ship(2, new float2(10f, 0f)) };
            ships[1].Revision = 42;

            ShipSpacingResult result = Solve(ships, 1);

            Assert.That(result.HasOverlap, Is.False);
            Assert.That(result.Saturated, Is.False);
            Assert.That(result.RegistrationId, Is.EqualTo(2));
            Assert.That(result.Revision, Is.EqualTo(42));
        }

        [Test]
        public void ResizeAndDispose_CompleteOutstandingBatchBeforeReleasingBuffers()
        {
            using var batch = new ShipSpacingBatch();
            batch.EnsureCapacity(2);
            NativeArray<ShipSpacingInput> inputs = batch.Inputs;
            inputs[0] = Ship(1, float2.zero);
            inputs[1] = Ship(2, float2.zero);
            batch.Schedule(2, 5f);
            batch.EnsureCapacity(32);
            Assert.That(batch.IsScheduled, Is.False);
            Assert.That(batch.Inputs.Length, Is.GreaterThanOrEqualTo(32));
            inputs = batch.Inputs;
            inputs[0] = Ship(1, float2.zero);
            inputs[1] = Ship(2, float2.zero);
            batch.Schedule(2, 5f);
        }

        private static ShipSpacingInput Ship(int id, float2 position) => new ShipSpacingInput
        {
            RegistrationId = id, Position = position, Radius = 5f,
            Bottom = -5f, Top = 5f, Eligible = true
        };

        private static ShipSpacingResult Solve(ShipSpacingInput[] ships, int index)
        {
            float maxRadius = 0f;
            foreach (ShipSpacingInput ship in ships) maxRadius = math.max(maxRadius, ship.Radius);
            float cellSize = math.max(1f, maxRadius * 2f);
            var cells = new Dictionary<int2, List<int>>();
            for (int i = 0; i < ships.Length; i++)
            {
                int2 cell = (int2)math.floor(ships[i].Position / cellSize);
                if (!cells.TryGetValue(cell, out List<int> indices))
                    cells.Add(cell, indices = new List<int>());
                indices.Add(i);
            }
            using var inputs = new NativeArray<ShipSpacingInput>(ships, Allocator.Temp);
            using var results = new NativeArray<ShipSpacingResult>(ships.Length, Allocator.Temp);
            using var occupants = new NativeArray<int>(ships.Length, Allocator.Temp);
            NativeArray<int> writableOccupants = occupants;
            using var ranges = new NativeParallelHashMap<int2, int2>(ships.Length, Allocator.Temp);
            using var occupancyHashes = new NativeParallelHashMap<int2, uint>(ships.Length, Allocator.Temp);
            int offset = 0;
            foreach (KeyValuePair<int2, List<int>> pair in cells)
            {
                ranges.Add(pair.Key, new int2(offset, pair.Value.Count));
                uint occupancyHash = 0;
                foreach (int occupant in pair.Value)
                {
                    writableOccupants[offset++] = occupant;
                    occupancyHash ^= ShipSpacingJob.HashOccupant(ships[occupant]);
                }
                occupancyHashes.Add(pair.Key, occupancyHash);
            }
            new ShipSpacingJob
            {
                Inputs = inputs, Results = results, Occupants = occupants,
                Ranges = ranges, OccupancyHashes = occupancyHashes, CellSize = cellSize
            }.Execute(index);
            return results[index];
        }
    }
}
