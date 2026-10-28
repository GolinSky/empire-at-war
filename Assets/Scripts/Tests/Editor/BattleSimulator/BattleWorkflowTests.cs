using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Editor.BattleSimulator;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Simulation")]
    public sealed class BattleWorkflowTests
    {
        [Test]
        public void Csv_QuotesNames_AndUsesInvariantNumbers()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
                var result = new BattleResult { Seed = 7, Duration = 2 };
                result.Units.Add(new UnitCombatStats { UnitId = "test", Name = "A, \"B\"", Hull = 12.5f, DamageDealt = 25 });
                string csv = BattleExport.UnitsCsv(result);
                Assert.That(csv, Does.Contain("\"A, \"\"B\"\"\""));
                Assert.That(csv, Does.Contain("12.5"));
                Assert.That(csv.Split('\n').Length, Is.EqualTo(3));
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [Test]
        public void CancelBeforeFirstSeed_ProducesExplicitPartialSummary()
        {
            var setup = BattleSimulationTests.Setup(); setup.Runs = 10;
            var runner = new MonteCarloRunner(setup, new Dictionary<string, SimUnitDefinition>(), null);
            runner.Cancel(); runner.Advance();
            Assert.That(runner.IsComplete, Is.True);
            Assert.That(runner.Summary.Completed, Is.Zero);
            Assert.That(BattleExport.Markdown(runner.Summary, setup), Does.Contain("cancelled True"));
        }

        [Test]
        public void SummaryUi_ContainsTimelineAndEveryUnit()
        {
            var result = new BattleResult { Seed = 1, Duration = 2, Winner = 0, Reason = "Elimination" };
            result.Units.Add(new UnitCombatStats { Name = "Alpha", Side = 0, Hull = 50, MaxHull = 100 });
            result.Units.Add(new UnitCombatStats { Name = "Beta", Side = 1, MaxHull = 100, DestroyedAt = 2 });
            var summary = new BattleSummary(); summary.Add(result);
            var view = new BattleSimulatorResultsView(summary);
            Assert.That(view.Q<BattleSimulatorTimelineChart>(), Is.Not.Null);
            Assert.That(view.Query<Label>().ToList().Any(l => l.text.Contains("Alpha")), Is.True);
        }
    }
}
