using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class FormationModelTests
    {
        [Test]
        public void CompactDestinations_RotateWithTravelDirectionAndClearOccupiedCenter()
        {
            var east = new List<FormationPoint>();
            var north = new List<FormationPoint>();
            var positions = new[] { new FormationPoint(0f, 0f), new FormationPoint(2f, 0f) };
            var rotated = new[] { new FormationPoint(0f, 0f), new FormationPoint(0f, 2f) };
            var radii = new[] { 5f, 5f };
            FormationModel.CalculateCompactDestinations(positions, radii,
                new FormationPoint(100f, 0f), east, 50f);
            FormationModel.CalculateCompactDestinations(rotated, radii,
                new FormationPoint(0f, 100f), north, 50f);
            for (int i = 0; i < east.Count; i++)
            {
                Assert.That(north[i].X, Is.EqualTo(-east[i].Z).Within(0.001f));
                Assert.That(north[i].Z, Is.EqualTo(east[i].X).Within(0.001f));
                Assert.That(FormationModel.HasClearance(east[i], 5f,
                    new FormationPoint(100f, 0f), 50f), Is.True);
            }
            Assert.That(FormationModel.HasClearance(east[0], 5f, east[1], 5f), Is.True);
        }

        private const int HEAVY_CRUISER = 4;
        private const int CAPITAL = 5;

        [Test]
        public void RowDestinations_HeavyCruiserLeadsWithoutCapitalsAndFollowsThem()
        {
            var positions = new[]
            {
                new FormationPoint(-20f, 0f), new FormationPoint(20f, 0f), new FormationPoint(60f, 0f)
            };
            var radii = new[] { 5f, 5f, 5f };
            var backRow = new[] { false, false, false };
            var target = new FormationPoint(0f, 500f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii,
                new[] { HEAVY_CRUISER, HEAVY_CRUISER, HEAVY_CRUISER }, backRow, target, destinations, rows);
            Assert.That(rows, Is.All.EqualTo(FormationRow.Front));

            FormationModel.CalculateRowDestinations(positions, radii, new[] { HEAVY_CRUISER, CAPITAL, CAPITAL },
                backRow, target, destinations, rows);
            Assert.That(rows, Is.EqualTo(new[] { FormationRow.Middle, FormationRow.Front, FormationRow.Front }));
            Assert.That(destinations[1].Z, Is.GreaterThan(destinations[0].Z), "The strongest tier present leads.");
            for (int i = 0; i < destinations.Count; i++)
            for (int j = i + 1; j < destinations.Count; j++)
                // Hull to hull: allow float rounding at contact.
                Assert.That(FormationModel.HasClearance(destinations[i], 4.99f, destinations[j], 4.99f), Is.True);
        }

        [Test]
        public void RowDestinations_FrontLineStandsOnThePointAndRowsStandBehindItWithAHullGap()
        {
            var positions = new[]
            {
                new FormationPoint(-30f, 0f), new FormationPoint(30f, 0f), new FormationPoint(0f, -40f),
                new FormationPoint(10f, -40f)
            };
            var radii = new[] { 50f, 50f, 10f, 10f };
            var tiers = new[] { CAPITAL, CAPITAL, 2, 2 };
            var backRow = new[] { false, false, false, false };
            var target = new FormationPoint(0f, 1000f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, target, destinations, rows);

            FormationPoint center = FormationModel.CalculateCenter(positions);
            float headingX = target.X - center.X;
            float headingZ = target.Z - center.Z;
            float length = (float)System.Math.Sqrt(headingX * headingX + headingZ * headingZ);
            float Depth(FormationPoint point) =>
                ((point.X - target.X) * headingX + (point.Z - target.Z) * headingZ) / length;

            Assert.That(Depth(destinations[0]), Is.EqualTo(0f).Within(0.01f), "No ship stands past the point.");
            Assert.That(Depth(destinations[1]), Is.EqualTo(0f).Within(0.01f));
            const float CLEARANCE = FormationModel.ROW_SLOT_CLEARANCE;
            Assert.That(CLEARANCE, Is.GreaterThan(1f), "Holding ships keep a gap between hulls.");
            Assert.That(Depth(destinations[2]), Is.LessThan(0f), "The smaller line stands behind the front line.");
            Assert.That(Depth(destinations[3]), Is.EqualTo(Depth(destinations[2])).Within(0.01f));
            AssertHullGaps(destinations, radii);
            Assert.That(-Depth(destinations[2]), Is.LessThan((radii[0] + radii[2]) * CLEARANCE),
                "Lines close up as far as the hulls ahead of them allow.");
            Assert.That(destinations[3].X - destinations[2].X, Is.EqualTo(radii[2] * 2f * CLEARANCE).Within(0.01f),
                "A line of small hulls spaces by its own hulls, not the row's largest.");
        }

        [Test]
        public void RowDestinations_OversizedHullTakesThePointAndTheOthersIgnoreIt()
        {
            // Battle 2026-11-27: spacing the group around a radius-895 dreadnought put every line ~1,400 behind the
            // point. The dreadnought flies in the deepest height layer, so it overlaps nobody.
            var positions = new[]
            {
                new FormationPoint(-200f, 0f), new FormationPoint(0f, 0f), new FormationPoint(200f, 0f),
                new FormationPoint(-100f, -50f), new FormationPoint(100f, -50f), new FormationPoint(0f, -100f)
            };
            var radii = new[] { 895f, 125f, 63f, 63f, 36f, 36f };
            var tiers = new[] { 7, 6, CAPITAL, CAPITAL, 4, 4 };
            var backRow = new[] { false, false, false, false, false, false };
            var target = new FormationPoint(0f, 1000f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();
            var withoutGiant = new List<FormationPoint>();
            var withoutGiantRows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, target, destinations, rows);
            FormationModel.CalculateRowDestinations(positions[1..], radii[1..], tiers[1..], backRow[1..], target,
                withoutGiant, withoutGiantRows);

            Assert.That(rows[0], Is.EqualTo(FormationRow.Front));
            Assert.That(destinations[0].X, Is.EqualTo(target.X).Within(0.01f), "The giant takes the point.");
            Assert.That(destinations[0].Z, Is.EqualTo(target.Z).Within(0.01f));
            for (int i = 1; i < positions.Length; i++)
            {
                Assert.That(rows[i], Is.EqualTo(withoutGiantRows[i - 1]));
                Assert.That(destinations[i].X, Is.EqualTo(withoutGiant[i - 1].X).Within(0.5f),
                    "The other slots are laid out as if the giant were not there.");
                Assert.That(destinations[i].Z, Is.EqualTo(withoutGiant[i - 1].Z).Within(0.5f));
            }
        }

        private static void AssertHullGaps(IReadOnlyList<FormationPoint> destinations, IReadOnlyList<float> radii)
        {
            for (int a = 0; a < destinations.Count; a++)
            for (int b = a + 1; b < destinations.Count; b++)
            {
                float dx = destinations[a].X - destinations[b].X;
                float dz = destinations[a].Z - destinations[b].Z;
                Assert.That((float)System.Math.Sqrt(dx * dx + dz * dz),
                    Is.GreaterThanOrEqualTo(FormationModel.SlotGap(radii[a], radii[b]) - 0.01f),
                    $"Slots {a} and {b} keep a hull gap.");
            }
        }

        [Test]
        public void RowDestinations_ALoneLargestShipJoinsTheNextTierInFront()
        {
            var positions = new[]
            {
                new FormationPoint(0f, 0f), new FormationPoint(-40f, 0f), new FormationPoint(40f, 0f),
                new FormationPoint(0f, -40f)
            };
            var radii = new[] { 30f, 10f, 10f, 5f };
            var tiers = new[] { 6, CAPITAL, CAPITAL, 2 };
            var backRow = new[] { false, false, false, false };
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, new FormationPoint(0f, 500f),
                destinations, rows);

            Assert.That(rows, Is.EqualTo(new[]
                { FormationRow.Front, FormationRow.Front, FormationRow.Front, FormationRow.Middle }));
            Assert.That(destinations[0].Z, Is.EqualTo(destinations[1].Z).Within(0.01f));
        }

        [Test]
        public void RowDestinations_LargerHullsTakeTheRowsFirstLine()
        {
            var positions = new List<FormationPoint>();
            var radii = new List<float>();
            var tiers = new List<int>();
            var backRow = new List<bool>();
            for (int i = 0; i < 2; i++) AddShip(i * 30f, -200f, 6);
            // Six cruisers ahead of six capitals: the capitals still take the middle row's first line.
            for (int i = 0; i < 6; i++) AddShip(i * 30f, 100f, 3);
            for (int i = 0; i < 6; i++) AddShip(i * 30f, -100f, CAPITAL);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, new FormationPoint(0f, 2000f),
                destinations, rows);

            for (int cruiser = 2; cruiser < 8; cruiser++)
            for (int capital = 8; capital < 14; capital++)
                Assert.That(destinations[capital].Z, Is.GreaterThan(destinations[cruiser].Z));

            void AddShip(float x, float z, int tier)
            {
                positions.Add(new FormationPoint(x, z));
                radii.Add(10f);
                tiers.Add(tier);
                backRow.Add(false);
            }
        }

        [Test]
        public void RowDestinations_BackRowShipsNeverLeadWhateverTheirTier()
        {
            var positions = new[]
            {
                new FormationPoint(0f, 40f), new FormationPoint(-30f, 0f), new FormationPoint(-10f, 0f),
                new FormationPoint(30f, 0f)
            };
            var radii = new[] { 8f, 5f, 5f, 5f };
            // Two front ships: a lone largest ship would join the next tier.
            var tiers = new[] { CAPITAL, HEAVY_CRUISER, HEAVY_CRUISER, 2 };
            var backRow = new[] { true, false, false, false };
            var target = new FormationPoint(0f, 800f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, target, destinations, rows);

            Assert.That(rows, Is.EqualTo(new[]
                { FormationRow.Back, FormationRow.Front, FormationRow.Front, FormationRow.Middle }));
            Assert.That(destinations[1].Z, Is.GreaterThan(destinations[3].Z));
            Assert.That(destinations[3].Z, Is.GreaterThan(destinations[0].Z));
            for (int i = 0; i < destinations.Count; i++)
            for (int j = i + 1; j < destinations.Count; j++)
                Assert.That(FormationModel.HasClearance(destinations[i], radii[i] * 0.999f, destinations[j],
                    radii[j] * 0.999f), Is.True, "Hull to hull, allowing float rounding at contact.");
        }

        [Test]
        public void RowDestinations_ShipsKeepTheirSideWithinARow()
        {
            var positions = new[] { new FormationPoint(40f, 0f), new FormationPoint(-40f, 0f), new FormationPoint(0f, -60f) };
            var radii = new[] { 5f, 5f, 5f };
            var tiers = new[] { CAPITAL, CAPITAL, 2 };
            var backRow = new[] { false, false, false };
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, new FormationPoint(0f, 500f),
                destinations, rows);

            Assert.That(destinations[0].X, Is.GreaterThan(destinations[1].X), "No crossing paths inside the row.");
        }

        [Test]
        public void RowDestinations_OneTierMatchesTheCompactClumpWithAHullGap()
        {
            var positions = new[] { new FormationPoint(0f, 0f), new FormationPoint(10f, 0f), new FormationPoint(20f, 5f) };
            var radii = new[] { 5f, 6f, 5f };
            var target = new FormationPoint(300f, 300f);
            var compact = new List<FormationPoint>();
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            var spaced = new float[radii.Length];
            for (int i = 0; i < radii.Length; i++) spaced[i] = radii[i] * FormationModel.ROW_SLOT_CLEARANCE;
            FormationModel.CalculateCompactDestinations(positions, spaced, target, compact);
            FormationModel.CalculateRowDestinations(positions, radii, new[] { 3, 3, 3 },
                new[] { false, false, false }, target, destinations, rows);

            Assert.That(destinations, Is.EqualTo(compact));
            Assert.That(rows, Is.All.EqualTo(FormationRow.Front));
        }

        [Test]
        public void CalculateDestination_PreservesOffsetsAroundTargetCenter()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(10f, 20f),
                new FormationPoint(14f, 24f)
            };
            FormationPoint center = FormationModel.CalculateCenter(positions);
            FormationPoint targetCenter = new FormationPoint(100f, 200f);

            FormationPoint first = FormationModel.CalculateDestination(positions[0], center, targetCenter);
            FormationPoint second = FormationModel.CalculateDestination(positions[1], center, targetCenter);

            Assert.That(first.X, Is.EqualTo(98f));
            Assert.That(first.Z, Is.EqualTo(198f));
            Assert.That(second.X, Is.EqualTo(102f));
            Assert.That(second.Z, Is.EqualTo(202f));
        }

        [Test]
        public void HasClearance_RejectsOverlappingShipRadii()
        {
            FormationPoint position = new FormationPoint(10f, -5f);

            Assert.That(
                FormationModel.HasClearance(
                    position,
                    5f,
                    position,
                    5f),
                Is.False);
            Assert.That(
                FormationModel.HasClearance(
                    position,
                    5f,
                    new FormationPoint(20f, -5f),
                    5f),
                Is.True);
        }

        [Test]
        public void CalculateCenter_ReturnsOriginForEmptyFormation()
        {
            FormationPoint center = FormationModel.CalculateCenter(new List<FormationPoint>());

            Assert.That(center.X, Is.Zero);
            Assert.That(center.Z, Is.Zero);
        }

        [Test]
        public void CalculateDestinations_PreservesDistinctOffsetsForSharedTarget()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-6f, 4f),
                new FormationPoint(6f, -4f)
            };
            List<FormationPoint> destinations = new List<FormationPoint>();

            FormationModel.CalculateDestinations(
                positions,
                new FormationPoint(100f, 200f),
                destinations);

            Assert.That(destinations, Has.Count.EqualTo(2));
            Assert.That(destinations[0].X, Is.EqualTo(94f));
            Assert.That(destinations[0].Z, Is.EqualTo(204f));
            Assert.That(destinations[1].X, Is.EqualTo(106f));
            Assert.That(destinations[1].Z, Is.EqualTo(196f));
        }

        [Test]
        public void CalculateGridDestination_AssignsDistinctCenteredSlots()
        {
            FormationPoint target = new FormationPoint(100f, 200f);

            FormationPoint first = FormationModel.CalculateGridDestination(0, 4, target, 12f);
            FormationPoint fourth = FormationModel.CalculateGridDestination(3, 4, target, 12f);

            Assert.That(first.X, Is.EqualTo(94f));
            Assert.That(first.Z, Is.EqualTo(194f));
            Assert.That(fourth.X, Is.EqualTo(106f));
            Assert.That(fourth.Z, Is.EqualTo(206f));
        }

        [Test]
        public void CalculateCompactDestinations_AssignsClosestShipToCenter()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-100f, 0f),
                new FormationPoint(8f, 0f)
            };
            List<float> radii = new List<float> { 5f, 5f };
            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationPoint target = new FormationPoint(10f, 0f);

            FormationModel.CalculateCompactDestinations(
                positions,
                radii,
                target,
                destinations);

            Assert.That(destinations, Has.Count.EqualTo(2));
            Assert.That(destinations[1].X, Is.EqualTo(target.X));
            Assert.That(destinations[1].Z, Is.EqualTo(target.Z));
            Assert.That(
                Distance(destinations[0], target),
                Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void CalculateCompactDestinations_SeparatesEveryShip()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-50f, 0f),
                new FormationPoint(0f, 50f),
                new FormationPoint(50f, 0f),
                new FormationPoint(0f, -50f)
            };
            List<float> radii = new List<float> { 4f, 6f, 8f, 5f };
            List<FormationPoint> destinations = new List<FormationPoint>();

            FormationModel.CalculateCompactDestinations(
                positions,
                radii,
                default,
                destinations);

            for (int first = 0; first < destinations.Count; first++)
            {
                for (int second = first + 1;
                     second < destinations.Count;
                     second++)
                {
                    Assert.That(
                        Distance(
                            destinations[first],
                            destinations[second]),
                        Is.GreaterThanOrEqualTo(
                            radii[first] + radii[second] - 0.001f));
                }
            }
        }

        private static float Distance(
            FormationPoint first,
            FormationPoint second)
        {
            float x = first.X - second.X;
            float z = first.Z - second.Z;
            return (float)System.Math.Sqrt(x * x + z * z);
        }
    }
}
