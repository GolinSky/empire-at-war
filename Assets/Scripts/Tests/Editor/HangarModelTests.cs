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

        private static HangarModel CreateModel(int reserve, int maxActive) =>
            new HangarModel(new TestHangarData(new HangarBay(SquadronType.Delta7, reserve, maxActive)));

        private sealed class TestHangarData : IHangarData
        {
            public TestHangarData(params HangarBay[] bays) => HangarBays = new List<HangarBay>(bays);

            public IReadOnlyList<HangarBay> HangarBays { get; }
            public float HangarInitialDelay => INITIAL_DELAY;
            public float HangarLaunchInterval => LAUNCH_INTERVAL;
        }
    }
}
