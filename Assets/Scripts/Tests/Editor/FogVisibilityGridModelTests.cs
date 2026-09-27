using EmpireAtWar.Models.FogOfWar;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FogVisibilityGridModelTests
    {
        [Test]
        public void Fade_MovesTowardRevealedTargetByMaxDelta()
        {
            FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.ResetTargets(false);
            grid.Reveal(4, 4, 2, 0f, 1f);

            Assert.That(grid.Fade(0.25f), Is.True);
            Assert.That(grid.GetVisibility(4, 4), Is.EqualTo(0.25f));
            Assert.That(grid.GetVisibility(0, 0), Is.EqualTo(0f));
        }

        [Test]
        public void ResetTargets_WithHistory_KeepsExploredCellsDimlyVisible()
        {
            FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
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
            FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.Reveal(4, 4, 2, 0f, 1f);
            grid.Fade(1f);

            grid.ResetTargets(false);
            grid.Fade(1f);

            Assert.That(grid.GetVisibility(4, 4), Is.EqualTo(0f));
            Assert.That(grid.Fade(1f), Is.False);
        }

        [Test]
        public void GetVisibility_OutsideGrid_SamplesClampedEdge()
        {
            FogVisibilityGridModel grid = new FogVisibilityGridModel(8);
            grid.Reveal(7, 0, 1, 0f, 1f);
            grid.Fade(1f);

            Assert.That(grid.GetVisibility(20, -5), Is.EqualTo(grid.GetVisibility(7, 0)));
            Assert.That(grid.GetVisibility(7, 0), Is.EqualTo(1f));
        }
    }
}
