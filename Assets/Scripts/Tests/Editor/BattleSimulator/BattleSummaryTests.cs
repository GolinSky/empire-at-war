using EmpireAtWar.Editor.BattleSimulator;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleSummaryTests
    {
        [Test]
        public void Summary_UsesInterpolatedPercentiles_AndExcludesTimeoutsFromTtk()
        {
            var summary = new BattleSummary();
            summary.Add(new BattleResult { Winner = 0, Duration = 10, Reason = "Elimination" });
            summary.Add(new BattleResult { Winner = 1, Duration = 20, Reason = "Elimination" });
            summary.Add(new BattleResult { Winner = -1, Duration = 600, Reason = "Timeout" });
            Assert.That(summary.Completed, Is.EqualTo(3));
            Assert.That(summary.WinsA, Is.EqualTo(1)); Assert.That(summary.WinsB, Is.EqualTo(1));
            Assert.That(summary.Draws, Is.EqualTo(1)); Assert.That(summary.Mean, Is.EqualTo(15));
            Assert.That(summary.Median, Is.EqualTo(15)); Assert.That(summary.P10, Is.EqualTo(11));
            Assert.That(summary.P90, Is.EqualTo(19));
        }
    }
}
