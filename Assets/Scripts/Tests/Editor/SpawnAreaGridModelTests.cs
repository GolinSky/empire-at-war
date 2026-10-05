using EmpireAtWar.Models.SpawnArea;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
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
        public void Stamp_CircleOutsideMap_IsClampedWithoutError()
        {
            SpawnAreaGridModel grid = CreateGrid();

            grid.Reveal(-490f, -490f, 300f);

            Assert.That(grid.GetAt(-495f, -495f), Is.EqualTo(SpawnAreaGridModel.OPEN));
        }
    }
}
