using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Components.Radar;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronTargetSelectorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<IEntity> _candidates = new List<IEntity>();
        private EntityLocator _locator;
        private VisionSpy _vision;

        [SetUp]
        public void SetUp()
        {
            _locator = new EntityLocator();
            _vision = new VisionSpy();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            _candidates.Clear();
        }

        [TestCase(false, ShipClass.Fighter)]
        [TestCase(true, ShipClass.Fighter)]
        [TestCase(false, ShipClass.Bomber)]
        [TestCase(true, ShipClass.Bomber)]
        public void Selection_PreservesClassPreferenceAndFirstTie(bool seen, ShipClass squadronClass)
        {
            ShipClass preferred = squadronClass == ShipClass.Bomber ? ShipClass.Capital : ShipClass.Fighter;
            ShipClass other = squadronClass == ShipClass.Bomber ? ShipClass.Fighter : ShipClass.Capital;
            AddTarget(6f, other);
            IEntity expected = AddTarget(10f, preferred);
            AddTarget(10f, preferred);

            Assert.That(Select(CreateSelector(squadronClass), seen), Is.SameAs(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Selection_RejectsWorseAndDistantCandidatesBeforeCloakOrVision(bool seen)
        {
            IEntity expected = AddTarget(3f);
            IEntity worse = AddTarget(20f);
            IEntity distant = AddTarget(200f);
            IEntity dead = AddTarget(1f);
            ((HealthStub)dead.HealthModel).IsDestroyed = true;
            IEntity ally = AddTarget(1f, owner: TestPlayers.Human);

            Assert.That(Select(CreateSelector(), seen), Is.SameAs(expected));
            Assert.That(Combat(expected).Reads, Is.EqualTo(1));
            foreach (IEntity rejected in new[] { worse, distant, dead, ally })
                Assert.That(Combat(rejected).Reads, Is.Zero);
            Assert.That(_vision.Reads, Is.EqualTo(1));
        }

        [TestCase(0, true)]
        [TestCase(2, true)]
        [TestCase(1, true)]
        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(1, false)]
        public void Selection_RejectsCloakedAndHiddenTargetsForEverySide(int owner, bool seen)
        {
            PlayerId side = new PlayerId(owner);
            PlayerId enemy = side == TestPlayers.Enemy ? TestPlayers.Human : TestPlayers.Enemy;
            IEntity cloaked = AddTarget(1f, owner: enemy);
            Combat(cloaked).Value.SetCloaked(true);
            IEntity hidden = AddTarget(2f, owner: enemy);
            _vision.Hidden.Add(hidden.GetFacade<IEntityTransformFacade>().Transform.position);
            IEntity visible = AddTarget(3f, owner: enemy);

            Assert.That(Select(CreateSelector(side: side), seen), Is.SameAs(visible));
            Assert.That(_vision.Reads, Is.EqualTo(2));
        }

        [Test]
        public void SelectNear_RechecksCloakAndHealthOnNextSearch()
        {
            IEntity first = AddTarget(1f);
            IEntity second = AddTarget(2f);
            SquadronTargetSelector selector = CreateSelector();
            Assert.That(Select(selector, false), Is.SameAs(first));
            Combat(first).Value.SetCloaked(true);
            Assert.That(Select(selector, false), Is.SameAs(second));
            ((HealthStub)second.HealthModel).IsDestroyed = true;
            Assert.That(Select(selector, false), Is.Null);
            Combat(first).Value.SetCloaked(false);
            Assert.That(Select(selector, false), Is.SameAs(first));
        }

        [Test]
        public void SelectNear_UsesRadarMembershipAndRadarTieOrder()
        {
            IEntity first = AddTarget(10f);
            IEntity second = AddTarget(10f);
            _candidates.Reverse();
            SquadronTargetSelector selector = CreateSelector();
            Assert.That(Select(selector, false), Is.SameAs(second));
            _candidates.Remove(second);
            Assert.That(Select(selector, false), Is.SameAs(first));
            _candidates.Clear();
            Assert.That(Select(selector, false), Is.Null);
        }

        [Test]
        public void SelectSeen_FindsTargetsAcrossNegativeAndPositiveCellEdges()
        {
            IEntity target = AddTarget(0f);
            target.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(-501f, 0f, 501f);
            SquadronTargetSelector selector = CreateSelector();
            Assert.That(selector.SelectSeen(new Vector3(-490f, 0f, 490f), 20f), Is.SameAs(target));
            Assert.That(selector.SelectSeen(new Vector3(-490f, 0f, 490f), 10f), Is.Null);
        }

        private IEntity Select(SquadronTargetSelector selector, bool seen) => seen
            ? selector.SelectSeen(Vector3.zero, 100f)
            : selector.SelectNear(new RadarStub(_candidates), Vector3.zero, 100f);

        [TestCase(false)]
        [TestCase(true)]
        public void Hunt_WithoutVisibleTargets_PersistsBeforeAndAfterAKill(bool hadTarget)
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject root = new GameObject("HuntingSquadron");
            _objects.Add(root);
            Squadron squadron = root.AddComponent<Squadron>();
            SquadronData data = ScriptableObject.CreateInstance<SquadronData>();
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                var flight = new FlightStub(data);
                var pilot = new SquadronPilot(flight);
                var weapon = new WeaponSpy();
                var orders = new UnitOrderModel();
                typeof(Squadron).GetField("_orders", PRIVATE_INSTANCE).SetValue(squadron, orders);
                typeof(Squadron).GetField("_flight", PRIVATE_INSTANCE).SetValue(squadron, flight);
                typeof(Squadron).GetField("_pilot", PRIVATE_INSTANCE).SetValue(squadron, pilot);
                typeof(Squadron).GetField("_weapon", PRIVATE_INSTANCE).SetValue(squadron, weapon);
                typeof(Squadron).GetField("_orderSettings", PRIVATE_INSTANCE).SetValue(squadron, settings);
                typeof(Squadron).GetField("_targetSelector", PRIVATE_INSTANCE).SetValue(squadron, CreateSelector());
                if (hadTarget)
                {
                    IEntity killed = AddTarget(1f);
                    pilot.Engage(killed);
                    ((HealthStub)killed.HealthModel).IsDestroyed = true;
                    typeof(Squadron).GetField("_engaged", PRIVATE_INSTANCE).SetValue(squadron, killed);
                }

                squadron.Hunt();
                MethodInfo update = typeof(Squadron).GetMethod("UpdateOrder", PRIVATE_INSTANCE);
                for (int i = 0; i < 3; i++) update.Invoke(squadron, new object[] { 1f });

                Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Hunt));
                Assert.That(typeof(Squadron).GetField("_engaged", PRIVATE_INSTANCE).GetValue(squadron), Is.Null);
                Assert.That(pilot.HasArrived, Is.True);
                Assert.That(weapon.ResetCount, Is.EqualTo(hadTarget ? 1 : 0));
                if (hadTarget) Assert.That(pilot.LoiterCenter, Is.EqualTo(flight.Centroid));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(UnitOrderType.Attack, UnitOrderType.None)]
        [TestCase(UnitOrderType.Guard, UnitOrderType.None)]
        [TestCase(UnitOrderType.Hunt, UnitOrderType.Hunt)]
        public void TargetDestroyedBetweenOrderCallbacks_LeavesEngageBeforePilotReadsView(
            UnitOrderType order, UnitOrderType expectedOrder)
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject root = new GameObject("Squadron with deferred orders");
            _objects.Add(root);
            Squadron squadron = root.AddComponent<Squadron>();
            SquadronData data = ScriptableObject.CreateInstance<SquadronData>();
            try
            {
                var flight = new FlightStub(data, 1);
                var pilot = new SquadronPilot(flight);
                var weapon = new WeaponSpy();
                var orders = new UnitOrderModel();
                IEntity target = AddTarget(1f);
                orders.Replace(order, target: target);
                pilot.Engage(target);
                typeof(Squadron).GetField("_orders", PRIVATE_INSTANCE).SetValue(squadron, orders);
                typeof(Squadron).GetField("_flight", PRIVATE_INSTANCE).SetValue(squadron, flight);
                typeof(Squadron).GetField("_pilot", PRIVATE_INSTANCE).SetValue(squadron, pilot);
                typeof(Squadron).GetField("_weapon", PRIVATE_INSTANCE).SetValue(squadron, weapon);
                typeof(Squadron).GetField("_engaged", PRIVATE_INSTANCE).SetValue(squadron, target);
                ((HealthStub)target.HealthModel).IsDestroyed = true;
                GameObject targetView = target.GetFacade<IEntityTransformFacade>().Transform.gameObject;
                _objects.Remove(targetView);
                UnityEngine.Object.DestroyImmediate(targetView);

                // No scheduler/order callback runs between target destruction and this frame.
                Assert.DoesNotThrow(squadron.Tick);

                Assert.That(orders.Current, Is.EqualTo(expectedOrder));
                Assert.That(typeof(Squadron).GetField("_engaged", PRIVATE_INSTANCE).GetValue(squadron), Is.Null);
                Assert.That(pilot.HasArrived, Is.True);
                Assert.That(weapon.ResetCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private sealed class FlightStub : ISquadronFlightComponent
        {
            public FlightStub(IFighterFlightData data, int count = 0) { Data = data; Count = count; }
            public string Id => "flight";
            public IFighterFlightData Data { get; }
            public int Count { get; }
            public Vector3 Centroid => new Vector3(10f, 0f, 20f);
            public Vector3 Heading => Vector3.forward;
            public bool IsAlive(int index) => true;
            public Vector3 GetPosition(int index) => Centroid;
            public Vector3 GetForward(int index) => Heading;
            public void Steer(int index, Vector3 target, float speed) { }
            public void Step(float deltaTime) { }
            public void Respawn(Vector3 position, Quaternion rotation) => throw new NotSupportedException();
        }

        private sealed class WeaponSpy : IWeaponComponent
        {
            public int ResetCount { get; private set; }
            public string Id => "weapon";
            public float AttackDistance => throw new NotSupportedException();
            public TargetPreference PreferredTarget => throw new NotSupportedException();
            public float GetEngagementRange(ShipClass targetClass) => throw new NotSupportedException();
            public void AddTarget(AttackData attackData, AttackType attackType) => throw new NotSupportedException();
            public bool HasTarget(IHealthFacade healthFacade) => throw new NotSupportedException();
            public bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass) => throw new NotSupportedException();
            public void ResetTarget() { ResetCount++; }
            public void Release() => throw new NotSupportedException();
        }

        private SquadronTargetSelector CreateSelector(ShipClass shipClass = ShipClass.Fighter, PlayerId? side = null)
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            return new SquadronTargetSelector(new SquadronTargetIndex(_locator, roster), _vision, roster,
                side ?? TestPlayers.Human, new SquadronHealthData { ShipClass = shipClass });
        }

        private IEntity AddTarget(float distance, ShipClass shipClass = ShipClass.Fighter, PlayerId? owner = null)
        {
            GameObject item = new GameObject("Target");
            _objects.Add(item);
            item.transform.position = Vector3.forward * distance;
            Entity entity = new Entity(new HealthStub { ShipClass = shipClass }, _locator,
                new IEntityFacade[] { new EntityTransformFacade(item.transform), new CombatSpy() },
                owner ?? TestPlayers.Enemy, _candidates.Count + 1, System.Array.Empty<IPooledEntityEvents>(),
                System.Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
            _locator.AddEntity(entity);
            _candidates.Add(entity);
            return entity;
        }

        private static CombatSpy Combat(IEntity entity) => (CombatSpy)entity.GetFacade<ICombatModifiersFacade>();

        private sealed class RadarStub : IRadarComponent
        {
            public string Id => "radar";
            public UnityEngine.Rendering.ObservableList<IEntity> Enemies { get; } =
                new UnityEngine.Rendering.ObservableList<IEntity>();
            public RadarStub(IList<IEntity> candidates)
            {
                foreach (IEntity candidate in candidates) Enemies.Add(candidate);
            }
            public bool HasEnemy(IEntity entity) => Enemies.Contains(entity);
        }

        private sealed class CombatSpy : ICombatModifiersFacade
        {
            public CombatModifiers Value { get; } = new CombatModifiers();
            public int Reads { get; private set; }
            public CombatModifiers Modifiers { get { Reads++; return Value; } }
        }

        private sealed class VisionSpy : IVisionService
        {
            public HashSet<Vector3> Hidden { get; } = new HashSet<Vector3>();
            public int Reads { get; private set; }
            public bool IsVisible(PlayerId viewer, Vector3 position) { Reads++; return !Hidden.Contains(position); }
            public IReadOnlyList<OwnedCircle> Sources => throw new NotSupportedException();
            public void Register(PlayerId owner, Transform source, float radius) => throw new NotSupportedException();
            public void Unregister(Transform source) => throw new NotSupportedException();
            public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) => throw new NotSupportedException();
        }

        private sealed class SquadronHealthData : ISquadronHealthData
        {
            public ShipClass ShipClass { get; set; }
            public float MemberHull => 1f;
            public float MemberShields => 0f;
            public float ShieldRegenerateValue => 0f;
            public float ShieldRegenerateDelay => 0f;
            public float LaserShieldDamageMultiplier => 1f;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; set; }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => !IsDestroyed;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Enemy;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
