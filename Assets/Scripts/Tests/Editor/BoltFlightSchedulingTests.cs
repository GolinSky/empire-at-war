using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class BoltFlightSchedulingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private BoltFlightBatch _flights;
        private Transform _muzzle;
        private Transform _target;
        private WeaponProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _flights = new BoltFlightBatch();
            _muzzle = Create("Muzzle").transform;
            _target = Create("Target").transform;
            _target.position = Vector3.forward * 100f;
            _profile = new WeaponProfile();
            JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":100}", _profile);
        }

        [TearDown]
        public void TearDown()
        {
            _flights.Dispose();
            foreach (GameObject instance in _objects) Object.DestroyImmediate(instance);
            _objects.Clear();
        }

        [Test]
        public void LateTick_CompletesCurrentFramePose_AndAdvanceStaysSynchronous()
        {
            BoltShot bolt = Fire();
            float launch = Time.time;
            _flights.ScheduleAdvance(launch + 0.5f, 0f);
            _flights.LateTick();
            AssertPosition(bolt, Vector3.forward * 50f);
            _flights.ScheduleAdvance(launch + 0.6f, 0f);
            _flights.Advance(launch + 0.75f, 0f);
            AssertPosition(bolt, Vector3.forward * 75f);
            _flights.LateTick();
            _flights.LateTick();
        }

        [Test]
        public void AddWhileScheduled_CompletesBeforeChangingTransformSlots()
        {
            BoltShot first = Fire();
            _flights.ScheduleAdvance(Time.time + 0.5f, 0f);
            BoltShot second = Fire();
            AssertPosition(first, Vector3.forward * 50f);
            AssertPosition(second, Vector3.zero);
            Assert.That(_flights.Count, Is.EqualTo(2));
            _flights.LateTick();
        }

        [Test]
        public void ReplayListedBolt_CompletesBeforeWritingLaunchTransform()
        {
            BoltShot bolt = Fire();
            _flights.ScheduleAdvance(Time.time + 0.5f, 0f);
            _muzzle.position = Vector3.right * 20f;
            // Exercise Play's replay boundary independently of ShotEffect's lease timer.
            typeof(BoltShot).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(bolt, new object[] { _muzzle, _target, Vector3.zero, _profile });
            _flights.LateTick();
            AssertPosition(bolt, _muzzle.position);
            Assert.That(_flights.Count, Is.EqualTo(1));
        }

        [Test]
        public void ResetWhileScheduled_CompletesBeforeReturningVisualToPool()
        {
            BoltShot bolt = Fire();
            _flights.ScheduleAdvance(Time.time + 0.5f, 0f);
            bolt.ResetForPool();
            bolt.transform.position = Vector3.right * 99f;
            _flights.LateTick();
            AssertPosition(bolt, Vector3.right * 99f);
            _flights.Advance(Time.time + 0.6f, 0f);
            Assert.That(_flights.Count, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearOrDisposeWhileScheduled_CompletesBeforeReleasingStorage(bool dispose)
        {
            BoltShot bolt = Fire();
            _flights.ScheduleAdvance(Time.time + 0.5f, 0f);
            if (dispose)
            {
                _flights.Dispose();
                _flights = new BoltFlightBatch();
            }
            else _flights.Clear();
            AssertPosition(bolt, Vector3.forward * 50f);
            Assert.That(_flights.Count, Is.Zero);
            _flights.LateTick();
        }

        [Test]
        public void Lifecycle_CompletesBeforeCombatAndDisposesBeforeEntities()
        {
            LifecycleOrderAttribute order = typeof(BoltFlightBatch).GetCustomAttribute<LifecycleOrderAttribute>();
            Assert.That(order.LateTick, Is.LessThan(LifecycleOrders.COMBAT_LATE_TICK));
            Assert.That(order.Dispose, Is.GreaterThan(LifecycleOrders.ENTITY_SCOPES_DISPOSE));
            Assert.That(_flights, Is.InstanceOf<ILateTickable>());
        }

        private GameObject Create(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        private BoltShot Fire()
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Vfx/Projectiles/Projectile.prefab"));
            _objects.Add(instance);
            BoltShot bolt = instance.GetComponent<BoltShot>();
            bolt.Fire(_muzzle, _target, _target, Vector3.zero, _profile, _flights);
            Assert.That(bolt.transform.parent, Is.Null, "Runtime pool spawns must retain independent roots.");
            return bolt;
        }

        private static void AssertPosition(BoltShot bolt, Vector3 expected) =>
            Assert.That(Vector3.Distance(bolt.transform.position, expected), Is.LessThan(0.01f));
    }
}
