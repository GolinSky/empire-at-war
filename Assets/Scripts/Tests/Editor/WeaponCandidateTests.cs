using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Tests.Editor;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using Utilities.ScriptUtils.Math;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class WeaponCandidateTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private CombatAttackCoordinator _coordinator;
        private WeaponComponent _weapon;
        private DebugRangeCircle _circle;

        [SetUp]
        public void SetUp()
        {
            _coordinator = new CombatAttackCoordinator();
            GameObject weaponObject = Track(new GameObject("Weapon"));
            _weapon = weaponObject.AddComponent<WeaponComponent>();
            typeof(WeaponComponent).GetField("_attackCoordinator", PRIVATE_INSTANCE).SetValue(_weapon, _coordinator);
            typeof(WeaponComponent).GetField("_visionService", PRIVATE_INSTANCE)
                .SetValue(_weapon, new VisionService(TestPlayers.CreateDuel(), false));
            typeof(WeaponComponent).GetField("_viewTransform", PRIVATE_INSTANCE).SetValue(_weapon, weaponObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_circle != null) _circle.Destroy();
            foreach (GameObject created in _objects)
                if (created != null) Object.DestroyImmediate(created);
            _objects.Clear();
            _coordinator.Dispose();
        }

        [TestCase(1)]
        [TestCase(174)]
        public void Tick_QueuesEveryReadyMountInOneFrame(int mountCount)
        {
            List<WeaponHardPoint> mounts = ConfigureMounts(mountCount);

            _weapon.Tick();

            TargetSelectionBatch batch = (TargetSelectionBatch)typeof(CombatAttackCoordinator)
                .GetField("_targetSelection", PRIVATE_INSTANCE).GetValue(_coordinator);
            Assert.That(batch.RequestCount, Is.EqualTo(mounts.Count));
        }

        [Test]
        public void NoEligibleTargets_DelaysOnlyThatMount()
        {
            List<WeaponHardPoint> mounts = ConfigureMounts(2);
            _weapon.CommitTargetSelection(mounts[0], _weapon.TargetVersion,
                new WeaponTargetSelectionJob.Result { CandidateIndex = -1, NearestDistance = float.PositiveInfinity },
                null, null, 0);

            _weapon.Tick();

            TargetSelectionBatch batch = (TargetSelectionBatch)typeof(CombatAttackCoordinator)
                .GetField("_targetSelection", PRIVATE_INSTANCE).GetValue(_coordinator);
            Assert.That(batch.RequestCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTargetSelection_RebuiltCandidates_FallsBackBeforeReadingCapturedGroupIndex()
        {
            ConfigureMounts(1);
            AttackData group = Group(1);
            group.Units[0].Transform.position = Vector3.forward * 10f;
            _weapon.AddTarget(group, AttackType.Base);
            _weapon.Tick();
            TargetSelectionBatch batch = (TargetSelectionBatch)typeof(CombatAttackCoordinator)
                .GetField("_targetSelection", PRIVATE_INSTANCE).GetValue(_coordinator);
            typeof(TargetSelectionBatch).GetMethod("CaptureTargetSelections", PRIVATE_INSTANCE).Invoke(batch, null);
            System.Collections.IList requests = (System.Collections.IList)typeof(TargetSelectionBatch)
                .GetField("_targetSelectionRequests", PRIVATE_INSTANCE).GetValue(batch);
            object capturedRequest = requests[0];
            Assert.That(_weapon.CandidateGroupCount, Is.EqualTo(1));

            ((HealthStub)group.TargetHealth).Destroy();
            Invoke("EnsureCandidates");
            Assert.That(_weapon.CandidateGroupCount, Is.Zero);

            Assert.DoesNotThrow(() => typeof(TargetSelectionBatch)
                .GetMethod("ApplyTargetSelection", PRIVATE_INSTANCE).Invoke(batch, new object[]
                {
                    capturedRequest, new WeaponTargetSelectionJob.Result { CandidateIndex = 0 }
                }));
            Assert.That(((float[])Field("_retryAt"))[0], Is.GreaterThan(Time.time));
            Assert.That(_coordinator.PendingSequences, Is.Zero);
        }

        [Test]
        public void CanAcquire_DestroyedTarget_DoesNotReadDestroyedTransform()
        {
            AttackData group = Group(1);
            ((HealthStub)group.TargetHealth).Destroy();
            Object.DestroyImmediate(group.Units[0].Transform.gameObject);

            bool canAcquire = (bool)typeof(WeaponComponent).GetMethod("CanAcquire", PRIVATE_INSTANCE)
                .Invoke(_weapon, new object[] { group });

            Assert.That(canAcquire, Is.False);
        }

        [Test]
        public void TurnedSalvo_DestroyedTarget_DoesNotReadDestroyedTransform()
        {
            WeaponHardPoint mount = ConfigureMounts(1)[0];
            AttackData group = Group(1);
            HardPointStub target = (HardPointStub)group.Units[0];
            target.Transform.position = Vector3.right * 10f;
            typeof(WeaponComponent).GetMethod("Engage", PRIVATE_INSTANCE)
                .Invoke(_weapon, new object[] { mount, group, target });
            System.Collections.IList salvos = (System.Collections.IList)Field("_turningSalvos");
            Assert.That(salvos.Count, Is.EqualTo(1));
            object salvo = salvos[0];
            salvo.GetType().GetField("FireTime").SetValue(salvo, float.NegativeInfinity);
            salvos[0] = salvo;
            ((HealthStub)group.TargetHealth).Destroy();
            Object.DestroyImmediate(target.Transform.gameObject);

            Assert.DoesNotThrow(() => Invoke("FireTurnedSalvos"));
            Assert.That(_coordinator.PendingSequences, Is.Zero);
            Assert.That(salvos.Count, Is.Zero);
        }

        [Test]
        public void TurnedSalvo_DoesNotFireAtRespawnedTarget()
        {
            WeaponHardPoint mount = ConfigureMounts(1)[0];
            AttackData group = Group(1);
            HardPointStub target = (HardPointStub)group.Units[0];
            target.Transform.position = Vector3.right * 10f;
            typeof(WeaponComponent).GetMethod("Engage", PRIVATE_INSTANCE)
                .Invoke(_weapon, new object[] { mount, group, target });
            System.Collections.IList salvos = (System.Collections.IList)Field("_turningSalvos");
            Assert.That(salvos.Count, Is.EqualTo(1));
            object salvo = salvos[0];
            salvo.GetType().GetField("FireTime").SetValue(salvo, float.NegativeInfinity);
            salvos[0] = salvo;
            target.Generation++;

            Assert.DoesNotThrow(() => Invoke("FireTurnedSalvos"));
            Assert.That(_coordinator.PendingSequences, Is.Zero);
            Assert.That(salvos.Count, Is.Zero);
        }

        [Test]
        public void Torpedo_RejectedShotDoesNotConsumeAttackRun()
        {
            ConfigureMounts(0);
            FighterTorpedoHardPoint mount = Track(new GameObject("Torpedo")).AddComponent<FighterTorpedoHardPoint>();
            CombatModifiers modifiers = new CombatModifiers();
            modifiers.SetCloaked(true);
            mount.SetData(Profile(), 100f, 0f, _weapon, _coordinator, modifiers, null, null, null, null);
            typeof(FighterTorpedoHardPoint).GetField("_attackRuns", PRIVATE_INSTANCE)
                .SetValue(mount, new AttackRunStub());
            AttackData group = Group(1);

            mount.Attack(group, group.Units[0]);

            Assert.That(typeof(FighterTorpedoHardPoint).GetField("_firedRun", PRIVATE_INSTANCE).GetValue(mount), Is.Zero);
            Assert.That(mount.CanEngage(ShipClass.Fighter), Is.False);
            Assert.That(mount.CanEngage(ShipClass.Capital), Is.True);
        }

        [TestCase(DamageType.Ion)]
        [TestCase(DamageType.IonCannon)]
        public void IonMount_DoesNotAcquireStrikecraft(DamageType damageType)
        {
            WeaponHardPoint mount = Track(new GameObject("Ion")).AddComponent<WeaponHardPoint>();
            mount.SetData(Profile(damageType), 100f, 0f, null, null, new CombatModifiers(), null, null, null, null);
            Assert.That(mount.CanEngage(ShipClass.Fighter), Is.False);
            Assert.That(mount.CanEngage(ShipClass.Bomber), Is.False);
            Assert.That(mount.CanEngage(ShipClass.Interceptor), Is.False);
            Assert.That(mount.CanEngage(ShipClass.Capital), Is.True);
        }

        private List<WeaponHardPoint> ConfigureMounts(int count)
        {
            CombatModifiers modifiers = new CombatModifiers();
            typeof(WeaponComponent).GetField("_modifiers", PRIVATE_INSTANCE).SetValue(_weapon, modifiers);
            WeaponModel model = new WeaponModel(new Dictionary<(DamageType, ShipClass), float>());
            model.SetRanges(100f, new Dictionary<ShipClass, float>());
            typeof(WeaponComponent).BaseType.GetMethod("SetModel", PRIVATE_INSTANCE)
                .Invoke(_weapon, new object[] { model });
            _circle = new DebugRangeCircle(new RangeDebugModel(), new SelectionModel(), null, "TestRange", Color.red);
            typeof(WeaponComponent).GetField("_attackRangeCircle", PRIVATE_INSTANCE).SetValue(_weapon, _circle);
            typeof(WeaponComponent).GetField("_viewTransform", PRIVATE_INSTANCE).SetValue(_weapon, _weapon.transform);
            List<WeaponHardPoint> mounts = new List<WeaponHardPoint>();
            for (int i = 0; i < count; i++)
            {
                WeaponHardPoint mount = Track(new GameObject("Mount")).AddComponent<WeaponHardPoint>();
                FloatRange arc = new FloatRange();
                typeof(Range<float>).GetProperty("Min").GetSetMethod(true).Invoke(arc, new object[] { -180f });
                typeof(Range<float>).GetProperty("Max").GetSetMethod(true).Invoke(arc, new object[] { 180f });
                mount.SetData(arc);
                mount.SetData(Profile(), 100f, 0f, _weapon, _coordinator, modifiers, null, null, null, null);
                object timer = typeof(WeaponHardPoint).GetField("_reloadTimer", PRIVATE_INSTANCE).GetValue(mount);
                timer.GetType().GetField("_timer", PRIVATE_INSTANCE).SetValue(timer, float.NegativeInfinity);
                mounts.Add(mount);
            }
            typeof(WeaponComponent).GetField("hardPoints", PRIVATE_INSTANCE).SetValue(_weapon, mounts);
            typeof(WeaponComponent).GetField("_retryAt", PRIVATE_INSTANCE).SetValue(_weapon, new float[count]);
            _coordinator.Register(_weapon);
            return mounts;
        }

        private static WeaponProfile Profile(DamageType damageType = DamageType.Laser) =>
            new WeaponProfile(WeaponType.Laser, "Test", WeaponFamily.Laser, damageType, 1f, 1, 0f, 1f, 100f,
                false, false, null, 100f, Color.white, Vector3.one, false, false, default, default, 45f);

        private sealed class AttackRunStub : IFighterAttackRunObserver
        {
            public int GetAttackRun(int fighterIndex) => 1;
        }

        [Test]
        public void AddBaseTarget_SplicesNewestContactAfterMainLikeARebuild()
        {
            AttackData first = Group(2);
            AttackData main = Group(2);
            AttackData newest = Group(3);
            _weapon.AddTarget(first, AttackType.Base);
            _weapon.AddTarget(main, AttackType.MainTarget);
            Invoke("EnsureCandidates");

            _weapon.AddTarget(newest, AttackType.Base);
            List<(AttackData, IHardPointModel)> spliced = Candidates();
            Invoke("RebuildCandidates");

            Assert.That(spliced, Is.EqualTo(Candidates()));
            Assert.That(spliced[2].Item1, Is.SameAs(newest));
        }

        [Test]
        public void MainTargetSwitches_KeepRebuildOrderWithoutRebuilding()
        {
            AttackData first = Group(2);
            AttackData second = Group(3);
            AttackData newMain = Group(2);
            _weapon.AddTarget(first, AttackType.Base);
            _weapon.AddTarget(second, AttackType.Base);
            Invoke("EnsureCandidates");

            // A new main target, then a listed group as main, then no main.
            _weapon.AddTarget(newMain, AttackType.MainTarget);
            AssertMatchesRebuild();
            _weapon.AddTarget(first, AttackType.MainTarget);
            AssertMatchesRebuild();
            _weapon.ResetTarget();
            AssertMatchesRebuild();
        }

        [Test]
        public void ListedGroups_SortNearestFirstBehindTheMainTarget()
        {
            AttackData far = Group(1);
            AttackData near = Group(2);
            AttackData main = Group(1);
            far.Units[0].Transform.position = new Vector3(0f, 0f, 50f);
            near.Units[0].Transform.position = new Vector3(0f, 0f, 10f);
            main.Units[0].Transform.position = new Vector3(0f, 0f, 90f);
            _weapon.AddTarget(far, AttackType.Base);
            _weapon.AddTarget(near, AttackType.Base);
            _weapon.AddTarget(main, AttackType.MainTarget);
            Invoke("EnsureCandidates");

            Invoke("SortListedGroupsByDistance");

            Assert.That((List<AttackData>)Field("_candidateGroups"), Is.EqualTo(new[] { main, near, far, main }));
            AssertMatchesRebuild();
        }

        [Test]
        public void ListedGroups_DestroyedTargetSortsLastWithoutReadingItsTransform()
        {
            AttackData dead = Group(1);
            AttackData far = Group(1);
            AttackData near = Group(1);
            far.Units[0].Transform.position = new Vector3(0f, 0f, 50f);
            near.Units[0].Transform.position = new Vector3(0f, 0f, 10f);
            _weapon.AddTarget(far, AttackType.Base);
            _weapon.AddTarget(dead, AttackType.Base);
            _weapon.AddTarget(near, AttackType.Base);
            Invoke("EnsureCandidates");
            ((HealthStub)dead.TargetHealth).Destroy();
            Object.DestroyImmediate(dead.Units[0].Transform.gameObject);

            Assert.DoesNotThrow(() => Invoke("SortListedGroupsByDistance"));
            Assert.That((List<AttackData>)Field("_candidateGroups"), Is.EqualTo(new[] { near, far, dead }));
        }

        private void AssertMatchesRebuild()
        {
            Assert.That(IsDirty(), Is.False);
            List<(AttackData, IHardPointModel)> switched = Candidates();
            Invoke("RebuildCandidates");
            Assert.That(switched, Is.EqualTo(Candidates()));
        }

        [Test]
        public void TargetDestroyed_RebuildsOnlyOnceHalfTheGroupsAreDead()
        {
            HealthStub[] healths = new HealthStub[4];
            for (int i = 0; i < healths.Length; i++)
            {
                healths[i] = new HealthStub(Units(1));
                _weapon.AddTarget(new AttackData(healths[i], null, HardPointType.Any, null), AttackType.Base);
            }

            Assert.That(IsDirty(), Is.False);
            healths[0].Destroy();
            Assert.That(IsDirty(), Is.False);
            healths[1].Destroy();
            Assert.That(IsDirty(), Is.True);
        }

        [Test]
        public void RespawnedTarget_IsListedAgainAfterItsGroupDied()
        {
            HealthStub health = new HealthStub(Units(1));
            HealthFacadeStub facade = new HealthFacadeStub();
            _weapon.AddTarget(new AttackData(health, facade, HardPointType.Any, null), AttackType.Base);
            _weapon.AddTarget(Group(1), AttackType.Base);
            _weapon.AddTarget(Group(1), AttackType.Base);

            health.Destroy();
            health.Respawn();

            Assert.That(_weapon.HasTarget(facade), Is.False);
            AttackData respawned = new AttackData(health, facade, HardPointType.Any, null);
            _weapon.AddTarget(respawned, AttackType.Base);
            Assert.That(((List<AttackData>)Field("_attackDataList")).Contains(respawned), Is.True);
        }

        private AttackData Group(int units)
        {
            HealthStub health = new HealthStub(Units(units));
            return new AttackData(health, new HealthFacadeStub(), HardPointType.Any,
                new EntityStub(health, health.GetShipUnits(HardPointType.Any)[0].Transform));
        }

        private sealed class EntityStub : IEntity
        {
            private readonly IEntityTransformFacade _transform;
            public EntityStub(IHealthModelObserver health, Transform transform)
            {
                HealthModel = health;
                _transform = new EntityTransformFacade(transform);
            }
            public long Id => 1;
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner => default;

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                _transform is TFacade facade ? facade : throw new InvalidOperationException("Missing fixture facade.");

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                facade = default;
                return false;
            }
        }

        private IHardPointModel[] Units(int count)
        {
            IHardPointModel[] units = new IHardPointModel[count];
            for (int i = 0; i < count; i++)
                units[i] = new HardPointStub(Track(new GameObject("Unit")).transform);
            return units;
        }

        private List<(AttackData, IHardPointModel)> Candidates()
        {
            int count = (int)Field("_candidateCount");
            int[] slots = (int[])Field("_candidateSlots");
            int[] groupIndices = (int[])Field("_candidateGroupIndices");
            List<AttackData> groups = (List<AttackData>)Field("_candidateGroups");
            List<(AttackData, IHardPointModel)> candidates = new List<(AttackData, IHardPointModel)>();
            for (int i = 0; i < count; i++)
                candidates.Add((groups[groupIndices[i]], _coordinator.TargetUnits.GetUnit(slots[i])));
            return candidates;
        }

        private bool IsDirty() => (bool)Field("_candidatesDirty");

        private object Field(string name) => typeof(WeaponComponent).GetField(name, PRIVATE_INSTANCE).GetValue(_weapon);

        private void Invoke(string method) =>
            typeof(WeaponComponent).GetMethod(method, PRIVATE_INSTANCE).Invoke(_weapon, null);

        private GameObject Track(GameObject created)
        {
            _objects.Add(created);
            return created;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            private readonly IHardPointModel[] _units;

            public HealthStub(IHardPointModel[] units) => _units = units;

            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => _units;

            public void Destroy()
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }

            public void Respawn() => IsDestroyed = false;
        }

        private sealed class HealthFacadeStub : IHealthFacade
        {
            public void ApplyDamage(float damage, DamageType damageType, int id) { }
        }

        private sealed class HardPointStub : IHardPointModel
        {
            public HardPointStub(Transform transform) => Transform = transform;

            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Weapon;
            public float HealthPercentage => 1f;
            public int Id => 0;
            public int Generation { get; set; }
            public bool IsDestroyed => false;
            public Vector3 Position => Transform.position;
            public Transform Transform { get; }
            public Transform Pivot => Transform;
        }
    }
}
