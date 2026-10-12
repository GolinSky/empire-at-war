using EmpireAtWar.Models.FogOfWar;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FogVisibilityGridModelTests
    {
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
