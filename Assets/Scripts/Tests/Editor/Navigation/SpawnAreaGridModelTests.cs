using EmpireAtWar.Models.SpawnArea;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class SpawnAreaGridModelTests
    {
        private const int RESOLUTION = 100;

        // 1000 x 1000 world units from (-500, -500): one cell is 10 units.
        private static SpawnAreaGridModel CreateGrid() => new SpawnAreaGridModel(RESOLUTION, -500f, -500f, 1000f, 1000f);

        [Test]
        public void Clear_EveryCellHidden()
        {
            SpawnAreaGridModel grid = CreateGrid();

            Assert.That(grid.GetAt(0f, 0f), Is.EqualTo(SpawnAreaGridModel.HIDDEN));
        }

        [Test]
        public void RevealAll_FogDisabledOpensWholeMapBeforeBlockers()
        {
            SpawnAreaGridModel grid = CreateGrid();
            grid.RevealAll();
            grid.Block(0f, 0f, 50f);

            Assert.That(grid.GetAt(-495f, -495f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(495f, 495f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(0f, 0f), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
        }

        [Test]
        public void Reveal_OpensCellsInsideCircleOnly()
        {
            SpawnAreaGridModel grid = CreateGrid();

            grid.Reveal(0f, 0f, 100f);

            Assert.That(grid.GetAt(0f, 0f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(90f, 0f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(150f, 0f), Is.EqualTo(SpawnAreaGridModel.HIDDEN));
        }

        [Test]
        public void Block_ClosesOnlyRevealedCells()
        {
            SpawnAreaGridModel grid = CreateGrid();
            grid.Reveal(0f, 0f, 100f);

            grid.Block(100f, 0f, 60f);

            Assert.That(grid.GetAt(80f, 0f), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
            Assert.That(grid.GetAt(0f, 0f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(150f, 0f), Is.EqualTo(SpawnAreaGridModel.HIDDEN));
        }

        [Test]
        public void Reveal_AfterBlock_KeepsBlockedCellsBlocked()
        {
            SpawnAreaGridModel grid = CreateGrid();
            grid.Reveal(0f, 0f, 100f);
            grid.Block(0f, 0f, 50f);

            grid.Reveal(0f, 0f, 100f);

            Assert.That(grid.GetAt(0f, 0f), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
        }

        [Test]
        public void BlockWhere_OnlyBlocksVisibleCellCentersInsideTheShape()
        {
            SpawnAreaGridModel grid = CreateGrid();
            grid.Reveal(0f, 0f, 20f);

            grid.BlockWhere(-35f, -35f, 35f, 35f, (x, z) => x > z);

            Assert.That(grid.GetAt(5f, -5f), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
            Assert.That(grid.GetAt(-5f, 5f), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.GetAt(25f, -5f), Is.EqualTo(SpawnAreaGridModel.HIDDEN));
        }

        [Test]
        public void Stamp_CircleOutsideMap_IsClampedWithoutError()
        {
            SpawnAreaGridModel grid = CreateGrid();

            grid.Reveal(-490f, -490f, 300f);

            Assert.That(grid.GetAt(-495f, -495f), Is.EqualTo(SpawnAreaGridModel.OPEN));
        }

        [Test]
        public void Snap_UsesTheSameCellCenterAsThePlacementPredicate()
        {
            SpawnAreaGridModel grid = CreateGrid();
            grid.RevealAll();
            float x = grid.SnapX(2f);
            float z = grid.SnapZ(-2f);

            grid.BlockWhere(x, z, x, z, (sampleX, sampleZ) => sampleX == x && sampleZ == z);

            Assert.That(x, Is.EqualTo(5f));
            Assert.That(z, Is.EqualTo(-5f));
            Assert.That(grid.GetAt(2f, -2f), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
            Assert.That(grid.GetAt(-2f, -2f), Is.EqualTo(SpawnAreaGridModel.OPEN));
        }

        [Test]
        public void BlockUnreachable_PreservesHiddenAndPreviouslyBlockedCells()
        {
            var grid = new SpawnAreaGridModel(2, 0f, 0f, 20f, 20f);
            grid.Reveal(5f, 5f, 1f);
            grid.Reveal(15f, 5f, 1f);
            grid.Reveal(5f, 15f, 1f);
            grid.Block(5f, 15f, 1f);

            grid.BlockUnreachable(new[] { false, true, true, false });

            Assert.That(grid.Get(0, 0), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
            Assert.That(grid.Get(1, 0), Is.EqualTo(SpawnAreaGridModel.OPEN));
            Assert.That(grid.Get(0, 1), Is.EqualTo(SpawnAreaGridModel.BLOCKED));
            Assert.That(grid.Get(1, 1), Is.EqualTo(SpawnAreaGridModel.HIDDEN));
        }

        [TestCase(-51f)]
        [TestCase(-50f)]
        [TestCase(-49f)]
        [TestCase(1f)]
        public void PlacementSpacing_PreservesMinimumSeparationAfterSnapping(float origin)
        {
            var grid = new SpawnAreaGridModel(100, -500f, -500f, 1000f, 2000f);
            const float MINIMUM = 21f;
            float spacing = grid.GetPlacementSpacing(MINIMUM);

            Assert.That(grid.SnapX(origin + spacing) - grid.SnapX(origin), Is.GreaterThanOrEqualTo(MINIMUM));
            Assert.That(grid.SnapZ(origin + spacing) - grid.SnapZ(origin), Is.GreaterThanOrEqualTo(MINIMUM));
        }
    }
}
