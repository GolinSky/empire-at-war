using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class BoltFlightSchedulingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private BoltFlightBatch _flights;
        private BoltShot _prefab;
        private Transform _muzzle;
        private Transform _target;
        private WeaponProfile _profile;
        private BoltTestRig.VisualHealth _health;
        private BoltTestRig.RecordingImpactView _impactView;
        private ImpactEffectPresenter _impacts;

        [SetUp]
        public void SetUp()
        {
            _flights = new BoltFlightBatch();
            _prefab = BoltTestRig.LoadBolt("Projectile");
            _muzzle = Create("Muzzle").transform;
            _target = Create("Target").transform;
            _target.position = Vector3.forward * 100f;
            _profile = BoltTestRig.SpeedProfile(100f);
            _health = new BoltTestRig.VisualHealth();
            _impactView = new BoltTestRig.RecordingImpactView();
            _impacts = BoltTestRig.Impacts(_impactView);
        }

        [TearDown]
        public void TearDown()
        {
            _flights.Dispose();
            foreach (GameObject instance in _objects) Object.DestroyImmediate(instance);
            _objects.Clear();
        }

        [Test]
        public void Launch_ReturnsTheShotPrefabFlightTime_AndLandsExactlyThen()
        {
            float launch = Time.time;
            float remaining = Fire(isHit: true);

            Assert.That(remaining, Is.EqualTo(_prefab.GetFlightSeconds(_muzzle.position, _target.position, _profile, 0f))
                .Within(0.0001f), "The bolt lands when combat scheduled the damage.");
            _flights.Advance(launch + remaining - 0.01f, 0f);
            Assert.That(_flights.Count, Is.EqualTo(1));
            Assert.That(_impactView.Positions, Is.Empty);
            _flights.Advance(launch + remaining, 0f);
            Assert.That(_flights.Count, Is.Zero);
            Assert.That(_impactView.Positions, Has.Count.EqualTo(1));
            Assert.That(Vector3.Distance(_impactView.Positions[0], _target.position), Is.LessThan(0.01f));
        }

        [Test]
        public void LateShot_StartsThatFarIntoItsFlight()
        {
            float launch = Time.time;
            float travel = _prefab.GetFlightSeconds(_muzzle.position, _target.position, _profile, 0f);
            float remaining = Fire(lateSeconds: 0.25f);

            Assert.That(remaining, Is.EqualTo(travel - 0.25f).Within(0.0001f));
            _flights.Advance(launch, 0f);
            Assert.That(Vector3.Distance(BoltTestRig.Position(Flight(0)), _target.position * (0.25f / travel)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void Miss_LandsWithoutAnImpact()
        {
            float launch = Time.time;
            float remaining = Fire(isHit: false);
            _flights.Advance(launch + remaining, 0f);
            Assert.That(_flights.Count, Is.Zero);
            Assert.That(_impactView.Positions, Is.Empty);
        }

        [Test]
        public void StrikecraftTarget_KeepsLaunchAim()
        {
            _health.ShipClass = ShipClass.Fighter;
            float launch = Time.time;
            float remaining = Fire(isHit: true);
            Vector3 aim = _target.position;
            _target.position = new Vector3(40f, 0f, 100f);
            _flights.Advance(launch + 0.5f, 0.016f);
            _flights.Advance(launch + remaining, 0.016f);
            Assert.That(Vector3.Distance(_impactView.Positions[0], aim), Is.LessThan(0.01f));
        }

        [Test]
        public void Advance_DrawsOneParticlePerBarrelOfEveryFlyingBolt()
        {
            float launch = Time.time;
            Fire();
            Fire();
            BoltLayer layer = BoltTestRig.Layer(_flights, _prefab);
            _flights.Advance(launch + 0.5f, 0f);
            Assert.That(layer.DrawnParticles, Is.EqualTo(2 * layer.Barrels));
            Assert.That(BoltTestRig.Particles(layer).particleCount, Is.EqualTo(2 * layer.Barrels));
            Assert.That(Vector3.Distance(BoltTestRig.Position(Flight(0)), Vector3.forward * 50f), Is.LessThan(0.01f));
        }

        [Test]
        public void LaunchBetweenTickAndPostTick_IsDrawnInTheSameFrame()
        {
            float launch = Time.time;
            Fire();
            _flights.AdvanceFlights(launch + 0.5f, 0f);
            Fire();
            _flights.PostTick();
            BoltLayer layer = BoltTestRig.Layer(_flights, _prefab);
            Assert.That(_flights.Count, Is.EqualTo(2));
            Assert.That(layer.DrawnParticles, Is.EqualTo(2 * layer.Barrels));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearOrDispose_DropsFlyingBoltsAndTheirParticles(bool dispose)
        {
            Fire(isHit: true);
            _flights.Advance(Time.time + 0.5f, 0f);
            BoltLayer layer = BoltTestRig.Layer(_flights, _prefab);
            if (dispose)
            {
                _flights.Dispose();
                _flights = new BoltFlightBatch();
            }
            else _flights.Clear();
            Assert.That(_flights.Count, Is.Zero);
            Assert.That(layer.DrawnParticles, Is.Zero);
            _flights.Advance(Time.time + 2f, 0f);
            Assert.That(_impactView.Positions, Is.Empty, "A dropped bolt never plays its impact.");
        }

        [Test]
        public void Lifecycle_DrawsAfterTickAndDisposesAfterEntities()
        {
            LifecycleOrderAttribute order = typeof(BoltFlightBatch).GetCustomAttribute<LifecycleOrderAttribute>();
            Assert.That(order.Dispose, Is.GreaterThan(LifecycleOrders.ENTITY_SCOPES_DISPOSE));
            Assert.That(_flights, Is.InstanceOf<ITickable>());
            Assert.That(_flights, Is.InstanceOf<IPostTickable>());
        }

        private GameObject Create(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        private float Fire(bool isHit = false, float lateSeconds = 0f) =>
            BoltTestRig.Launch(_flights, _prefab, _profile, _muzzle, _target, _health, _impacts, isHit, lateSeconds);

        private BoltFlight Flight(int index) => BoltTestRig.Layer(_flights, _prefab).Flights[index];
    }
}
