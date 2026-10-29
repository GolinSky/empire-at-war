using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class HangarModelTests
    {
        private const float INITIAL_DELAY = 4f;
        private const float LAUNCH_INTERVAL = 8f;

        [TestCase(false)]
        [TestCase(true)]
        public void Upgrade_RepairsHangarAndResumesReserveLaunches(bool individualBay)
        {
            var root = new GameObject("Hangar repair test");
            var data = new TestHangarData(new HangarBay(SquadronType.Delta7, 3, 1));
            var health = new HealthModel(data, null, new CombatModifiers(), new HardPointHealthBudget());
            var hardPointModel = new HardPointModel(HardPointType.Hangar, 0);
            health.InitializeHardPoints(new[] { hardPointModel });
            var adapter = new HardPointAdapter(new HardPointViewStub(root.transform), hardPointModel, root.transform);
            var component = root.AddComponent<HangarComponent>();
            try
            {
                var healthComponent = root.AddComponent<HealthComponent>();
                SetField(healthComponent, "_hardPointAdapters", new[] { adapter });
                var hardPoint = root.AddComponent<HardPoint>();
                SetField(component, "hangarHardPoint", hardPoint);
                if (individualBay) SetField(component, "bayHardPoints", new[] { hardPoint });
                var model = new HangarModel(data);
                typeof(HangarComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(component, new object[] { data, healthComponent, model, null, null, default(EmpireAtWar.Models.Players.PlayerId) });
                component.Initialize();
                Assert.That(model.TryLaunch(INITIAL_DELAY, out _), Is.True);
                hardPointModel.ApplyDamage(hardPointModel.MaxHealth);
                model.SquadronLost(0);
                Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False);

                health.Upgrade(2, 1f, 1f, 1f);

                Assert.That(model.IsOperational, Is.True);
                Assert.That(model.GetReserve(0), Is.EqualTo(2));
                Assert.That(model.GetActive(0), Is.Zero);
                Assert.That(model.Cooldown, Is.EqualTo(LAUNCH_INTERVAL));
                Assert.That(model.TryLaunch(HangarModel.RELAUNCH_COOLDOWN, out _), Is.True);
                Assert.That(model.GetReserve(0), Is.EqualTo(1));
                component.Release();
                hardPointModel.SetHealth(100f, 1f);
                Assert.That(model.IsOperational, Is.False, "Released hangars must stop observing repairs.");
            }
            finally
            {
                component.Release();
                adapter.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class HardPointViewStub : IHardPoint
        {
            public bool IsDestroyed => false;
            public int Id => 0;
            public HardPointType HardPointType => HardPointType.Hangar;
            public Vector3 Position => Transform.position;
            public Transform Transform { get; }
            public HardPointViewStub(Transform transform) => Transform = transform;
            public bool TryGetWeaponType(out WeaponType weaponType)
            {
                weaponType = default;
                return false;
            }
            public void UpdateData(float healthPercentage) { }
        }

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
        public void AdvanceCooldown_RunsTimerWithoutLaunching()
        {
            HangarModel model = CreateModel(reserve: 3, maxActive: 2);

            model.AdvanceCooldown(INITIAL_DELAY);

            Assert.That(model.GetActive(0), Is.EqualTo(0));
            Assert.That(model.TryLaunch(0f, out _), Is.True);
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
        public void SquadronLost_ReplacesOnlyOnceTheLostLaunchIsAMinuteOld()
        {
            HangarModel model = CreateModel(reserve: 2, maxActive: 1);
            model.TryLaunch(INITIAL_DELAY, out _);
            model.TryLaunch(30f, out _);

            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False, "A squadron lost 30 s after launch is not replaced at once.");
            Assert.That(model.TryLaunch(HangarModel.RELAUNCH_COOLDOWN - 30f - LAUNCH_INTERVAL - 0.1f, out _), Is.False);
            Assert.That(model.TryLaunch(0.2f, out _), Is.True);
            Assert.That(model.GetReserve(0), Is.EqualTo(0));
        }

        [Test]
        public void SquadronLost_LongLivedSquadronIsReplacedAfterTheLaunchInterval()
        {
            HangarModel model = CreateModel(reserve: 2, maxActive: 1);
            model.TryLaunch(INITIAL_DELAY, out _);
            model.TryLaunch(HangarModel.RELAUNCH_COOLDOWN, out _);

            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL - 0.1f, out _), Is.False);
            Assert.That(model.TryLaunch(0.2f, out _), Is.True);
        }

        [Test]
        public void TwoSlotBay_LaunchesBothAtOnce_ThenWaitsAMinuteFromTheOldestLaunch()
        {
            HangarModel model = CreateModel(reserve: 5, maxActive: 2);
            Assert.That(model.TryLaunch(INITIAL_DELAY, out _), Is.True);
            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.True, "The second slot does not wait for the first launch.");

            model.SquadronLost(0);

            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False);
            Assert.That(model.TryLaunch(HangarModel.RELAUNCH_COOLDOWN - 2f * LAUNCH_INTERVAL - 0.1f, out _), Is.False);
            Assert.That(model.TryLaunch(0.2f, out _), Is.True);
        }

        [Test]
        public void AbilityLaunches_IgnoreTheRelaunchCooldown()
        {
            HangarModel model = CreateModel(reserve: 3, maxActive: 1);
            model.TryLaunch(INITIAL_DELAY, out _);
            model.SquadronLost(0);

            Assert.That(model.TryLaunchImmediately(out _), Is.True);
            model.SquadronLost(0);
            Assert.That(model.TryLaunchReplacement(out _), Is.True);
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

        [Test]
        public void Restore_LoadsReserveAndTimer_AdoptedSquadronsHoldTheirBay()
        {
            HangarModel model = CreateModel(reserve: 5, maxActive: 1);

            model.Restore(new[] { 2 }, 1f);
            model.AdoptLaunched(0);

            Assert.That(model.GetReserve(0), Is.EqualTo(2));
            Assert.That(model.Cooldown, Is.EqualTo(1f));
            Assert.That(model.TryLaunch(1f, out _), Is.False, "The bay's one active slot is taken by the adopted squadron.");
            model.SquadronLost(0);
            Assert.That(model.TryLaunch(LAUNCH_INTERVAL, out _), Is.False, "An adopted squadron counts as just launched.");
            Assert.That(model.TryLaunch(HangarModel.RELAUNCH_COOLDOWN, out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(0));
            Assert.That(model.GetReserve(0), Is.EqualTo(1));
        }

        [Test]
        public void Restore_BayCountMismatch_Throws()
        {
            HangarModel model = CreateModel(reserve: 5, maxActive: 1);

            Assert.Throws<System.InvalidOperationException>(() => model.Restore(new[] { 1, 1 }, 0f));
        }

        private sealed class TestHangarData : IHangarData, IHealthData
        {
            public ShipClass ShipClass => ShipClass.Structure;
            public float Hull => 1000f;
            public float Shields => 0f;
            public float ShieldRegenerateValue => 0f;
            public float ShieldRegenerateDelay => 0f;
            public bool UsesHullHardPointBudget => false;
            public IReadOnlyList<HardPointHealth> HardPointHealth { get; } = new[]
            {
                JsonUtility.FromJson<HardPointHealth>($"{{\"hardPointType\":{(int)HardPointType.Hangar},\"health\":100,\"hullDamageMultiplier\":1}}")
            };
            public IReadOnlyList<HangarBay> HangarBays { get; }
            public float HangarInitialDelay => INITIAL_DELAY;
            public float HangarLaunchInterval => LAUNCH_INTERVAL;

            public TestHangarData(params HangarBay[] bays) => HangarBays = new List<HangarBay>(bays);
        }
    }
}
