using EmpireAtWar.Entities.TacticalMarkers;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class MarkerRadialModelTests
    {
        [TestCase(0f, 100f, 0)]
        [TestCase(100f, 50f, 1)]
        [TestCase(100f, -50f, 2)]
        [TestCase(0f, -100f, 3)]
        [TestCase(-100f, -50f, 4)]
        [TestCase(-100f, 50f, 5)]
        public void SelectionUsesClockwiseMarkerOrder(float x, float y, int expected)
        {
            var model = new MarkerRadialModel(); model.Open(); model.Select(x, y);
            Assert.That(model.SelectedIndex, Is.EqualTo(expected));
        }

        [Test]
        public void ClampedMenuStartsNeutralUntilPointerMovesThenUsesDisplayedCenter()
        {
            var model = new MarkerRadialModel(); model.Open(-150, -100);
            model.Select(-149, -99);
            Assert.That(model.SelectedIndex, Is.EqualTo(-1));
            model.Select(0, 100);
            Assert.That(model.SelectedIndex, Is.EqualTo(0));
            model.Select(0, 0);
            Assert.That(model.SelectedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void ReturningToHubCancelsSelectionAndClosingResetsState()
        {
            var model = new MarkerRadialModel(); model.Open(); model.Select(100, 0);
            model.Select(0, 0);
            Assert.That(model.SelectedIndex, Is.EqualTo(-1));
            model.Close();
            Assert.That(model.IsOpen, Is.False);
            model.Open();
            Assert.That(model.SelectedIndex, Is.EqualTo(-1));
        }
    }
}
