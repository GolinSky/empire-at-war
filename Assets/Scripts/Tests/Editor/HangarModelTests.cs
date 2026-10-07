using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Squadrons;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HangarModelTests
    {
        private const float INITIAL_DELAY = 4f;
        private const float LAUNCH_INTERVAL = 8f;

        [Test]
        public void TryLaunch_WaitsForInitialDelay()
        {
            HangarModel model = CreateModel(reserve: 3, maxActive: 2);

            Assert.That(model.TryLaunch(INITIAL_DELAY - 0.1f, out _), Is.False);
            Assert.That(model.TryLaunch(0.2f, out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(0));
            Assert.That(model.GetReserve(0), Is.EqualTo(2));
            Assert.That(model.GetActive(0), Is.EqualTo(1));
        }

        [Test]
        public void TryLaunch_StopsAtMaxActive()
        {
            HangarModel model = CreateModel(reserve: 5, maxActive: 2);

            Assert.That(model.TryLaunch(INITIAL_DELAY, out _), Is.True);
            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.True);
            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False);
            Assert.That(model.GetActive(0), Is.EqualTo(2));
            Assert.That(model.GetReserve(0), Is.EqualTo(3));
        }

        [Test]
        public void SquadronLost_ReplacesFromReserveAfterInterval()
        {
            HangarModel model = CreateModel(reserve: 2, maxActive: 1);
            model.TryLaunch(INITIAL_DELAY, out _);

            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL - 0.1f, out _), Is.False);
            Assert.That(model.TryLaunch(0.2f, out _), Is.True);
            Assert.That(model.GetReserve(0), Is.EqualTo(0));
        }

        [Test]
        public void TryLaunch_EmptyReserveLaunchesNothing()
        {
            HangarModel model = CreateModel(reserve: 1, maxActive: 2);
            model.TryLaunch(INITIAL_DELAY, out _);
            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL * 10f, out _), Is.False);
        }

        [Test]
        public void Shutdown_StopsLaunching()
        {
            HangarModel model = CreateModel(reserve: 4, maxActive: 2);

            model.Shutdown();

            Assert.That(model.TryLaunch(INITIAL_DELAY * 10f, out _), Is.False);
            Assert.That(model.IsOperational, Is.False);
        }

        [Test]
        public void DisableBay_StopsOnlyItsLaunches()
        {
            var model = new HangarModel(new TestHangarData(
                new HangarBay(SquadronType.XWing, 3, 1),
                new HangarBay(SquadronType.YWing, 3, 1),
                new HangarBay(SquadronType.AWing, 3, 1)));

            model.DisableBay(0);
            Assert.That(model.TryLaunch(INITIAL_DELAY, out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(1));
            Assert.That(model.GetReserve(0), Is.EqualTo(3));
            model.DisableBay(1);
            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out bay), Is.True);
            Assert.That(bay, Is.EqualTo(2));
            Assert.That(model.IsOperational, Is.True);
        }

        [Test]
        public void DisableBay_AllDestroyedStopsLaunchingAndReplacement()
        {
            var model = new HangarModel(new TestHangarData(
                new HangarBay(SquadronType.XWing, 3, 1),
                new HangarBay(SquadronType.YWing, 3, 1)));
            model.TryLaunch(INITIAL_DELAY, out _);

            model.DisableBay(0);
            model.DisableBay(1);
            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False);
            Assert.That(model.IsOperational, Is.False);
            Assert.That(model.GetReserve(0), Is.EqualTo(2));
            Assert.That(model.GetReserve(1), Is.EqualTo(3));
        }

        private static HangarModel CreateModel(int reserve, int maxActive) =>
            new HangarModel(new TestHangarData(new HangarBay(SquadronType.Delta7, reserve, maxActive)));

        private sealed class TestHangarData : IHangarData
        {
            public IReadOnlyList<HangarBay> HangarBays { get; }
            public float HangarInitialDelay => INITIAL_DELAY;
            public float HangarLaunchInterval => LAUNCH_INTERVAL;

            public TestHangarData(params HangarBay[] bays) => HangarBays = new List<HangarBay>(bays);
        }
    }
}
