using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Repair;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class RepairServiceTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private EntityLocator _entities;
        private RepairService _service;
        private long _nextId;

        [SetUp]
        public void SetUp()
        {
            _entities = new EntityLocator();
            _nextId = 1;
        }

        [TearDown]
        public void TearDown()
        {
            _service?.LateDispose();
            _service = null;
            foreach (UnityEngine.Object created in _created) UnityEngine.Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void RepairsOnlyDamagedOwnAndAlliedUnitsInsideTheRadius()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            RepairProfile profile = CreateProfile(radius: 100f);
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, profile, hullPercentage: 0.5f);
            FakeEntity own = AddUnit(TestPlayers.Human, new Vector3(50f, 0f, 0f), 0.5f);
            FakeEntity ally = AddUnit(TestPlayers.Ally, new Vector3(0f, 0f, 99f), 0.5f);
            FakeEntity enemy = AddUnit(TestPlayers.Enemy, new Vector3(10f, 0f, 0f), 0.5f);
            FakeEntity neutral = AddUnit(PlayerId.None, new Vector3(10f, 0f, 0f), 0.5f);
            FakeEntity outside = AddUnit(TestPlayers.Human, new Vector3(101f, 0f, 0f), 0.5f);
            FakeEntity full = AddUnit(TestPlayers.Human, new Vector3(10f, 0f, 0f), 1f);
            FakeEntity dead = AddUnit(TestPlayers.Human, new Vector3(10f, 0f, 0f), 0.5f);
            dead.Health.IsDestroyed = true;
            FakeEntity squadron = AddEntity(TestPlayers.Human, new Vector3(10f, 0f, 0f), null);

            _service.Advance(0.1f);

            Assert.That(own.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(ally.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(own.Repairable.HullPercentage, Is.EqualTo(0.5f + profile.FractionPerTick).Within(0.0001f));
            foreach (FakeEntity untouched in new[] { repairer, enemy, neutral, outside, full, dead })
                Assert.That(untouched.Repairable.RepairCount, Is.Zero);
            Assert.That(repairer.Source.Targets, Is.EquivalentTo(new IEntity[] { own, ally }));
            Assert.That(repairer.Source.Targets, Has.No.Member(squadron));
        }

        [Test]
        public void RadiusIgnoresTheTargetsFlightHeight()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            RepairProfile profile = CreateProfile(radius: 100f);
            AddRepairer(TestPlayers.Human, Vector3.zero, profile);
            // Dreadnoughts fly far below the plane a satellite sits on; only the flat ring counts.
            FakeEntity deepInside = AddUnit(TestPlayers.Human, new Vector3(90f, -400f, 0f), 0.5f);
            FakeEntity highInside = AddUnit(TestPlayers.Human, new Vector3(0f, 300f, 90f), 0.5f);
            FakeEntity deepOutside = AddUnit(TestPlayers.Human, new Vector3(101f, -400f, 0f), 0.5f);

            _service.Advance(0.1f);

            Assert.That(deepInside.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(highInside.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(deepOutside.Repairable.RepairCount, Is.Zero);
        }

        [Test]
        public void FillsUpToMaxTargetsMostDamagedFirst()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile(maxTargets: 2));
            FakeEntity light = AddUnit(TestPlayers.Human, Vector3.right, 0.7f);
            FakeEntity heavy = AddUnit(TestPlayers.Human, Vector3.right, 0.3f);
            FakeEntity medium = AddUnit(TestPlayers.Human, Vector3.right, 0.5f);

            _service.Advance(0.1f);

            Assert.That(repairer.Source.Targets, Is.EqualTo(new IEntity[] { heavy, medium }));
            Assert.That(light.Repairable.RepairCount, Is.Zero);
        }

        [Test]
        public void RepairerOverTheHealerLimitPicksAnotherTarget()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            RepairProfile profile = CreateProfile(interval: 1f, maxTargets: 1, maxHealers: 2);
            FakeEntity first = AddRepairer(TestPlayers.Human, Vector3.zero, profile);
            FakeEntity second = AddRepairer(TestPlayers.Human, Vector3.zero, profile);
            FakeEntity third = AddRepairer(TestPlayers.Human, Vector3.zero, profile);
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.2f);

            _service.Advance(1f);

            Assert.That(_service.GetHealerCount(target.Id), Is.EqualTo(2));
            Assert.That(target.Repairable.RepairCount, Is.EqualTo(2));
            Assert.That(first.Source.Targets, Is.EqualTo(new IEntity[] { target }));
            Assert.That(second.Source.Targets, Is.EqualTo(new IEntity[] { target }));
            Assert.That(third.Source.Targets, Is.Empty);

            FakeEntity other = AddUnit(TestPlayers.Human, Vector3.right, 0.6f);
            _service.Advance(1f);

            Assert.That(third.Source.Targets, Is.EqualTo(new IEntity[] { other }));
            Assert.That(_service.GetHealerCount(target.Id), Is.EqualTo(2));
        }

        [Test]
        public void KeepsATargetUntilItIsFullThenTakesTheNextMostDamaged()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero,
                CreateProfile(interval: 1f, percentPerTick: 30f, maxTargets: 1));
            FakeEntity first = AddUnit(TestPlayers.Human, Vector3.right, 0.5f);

            _service.Advance(1f);
            FakeEntity worse = AddUnit(TestPlayers.Human, Vector3.right, 0.2f);
            _service.Advance(1f);

            Assert.That(repairer.Source.Targets, Is.EqualTo(new IEntity[] { first }));
            Assert.That(first.Repairable.HullPercentage, Is.EqualTo(1f));
            Assert.That(worse.Repairable.RepairCount, Is.Zero);

            _service.Advance(1f);

            Assert.That(repairer.Source.Targets, Is.EqualTo(new IEntity[] { worse }));
            Assert.That(_service.GetHealerCount(first.Id), Is.Zero);
            Assert.That(worse.Repairable.RepairCount, Is.EqualTo(1));
        }

        [Test]
        public void RepairsOncePerInterval()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile(interval: 2f));
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.1f);

            _service.Advance(0.1f);
            _service.Advance(1f);
            Assert.That(target.Repairable.RepairCount, Is.EqualTo(1));

            _service.Advance(1f);
            Assert.That(target.Repairable.RepairCount, Is.EqualTo(2));

            _service.Advance(10f);
            Assert.That(target.Repairable.RepairCount, Is.EqualTo(3), "A long frame must not burst catch-up repairs.");
        }

        [Test]
        public void DisabledRepairerStopsAndFreesItsTargets()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile(interval: 1f));
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.1f);
            _service.Advance(1f);
            Assert.That(_service.GetHealerCount(target.Id), Is.EqualTo(1));

            repairer.Source.CanRepair = false;
            _service.Advance(1f);

            Assert.That(target.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(_service.GetHealerCount(target.Id), Is.Zero);
            Assert.That(repairer.Source.Targets, Is.Empty);
        }

        [Test]
        public void OnlineClientDoesNotRepair()
        {
            CreateService(new OnlineSession(false, TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile());
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.1f);

            _service.Advance(5f);

            Assert.That(target.Repairable.RepairCount, Is.Zero);
            Assert.That(repairer.Source.Targets, Is.Empty);
        }

        [Test]
        public void RemovedTargetFreesItsClaim()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile());
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.1f);
            _service.Advance(0.1f);

            _entities.RemoveEntity(target);

            Assert.That(_service.GetHealerCount(target.Id), Is.Zero);
            Assert.That(repairer.Source.Targets, Is.Empty);
        }

        [Test]
        public void RemovedRepairerFreesItsClaims()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeEntity repairer = AddRepairer(TestPlayers.Human, Vector3.zero, CreateProfile());
            FakeEntity target = AddUnit(TestPlayers.Human, Vector3.right, 0.1f);
            _service.Advance(0.1f);

            _entities.RemoveEntity(repairer);
            _service.Advance(5f);

            Assert.That(_service.GetHealerCount(target.Id), Is.Zero);
            Assert.That(target.Repairable.RepairCount, Is.EqualTo(1));
        }

        private void CreateService(IMatchSession session)
        {
            _service = new RepairService(_entities, TestPlayers.CreateTeamGame(), session);
            _service.Initialize();
        }

        private RepairProfile CreateProfile(float radius = 100f, float interval = 1f, float percentPerTick = 1f,
            int maxTargets = 10, int maxHealers = 2)
        {
            RepairProfile profile = ScriptableObject.CreateInstance<RepairProfile>();
            _created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("<Radius>k__BackingField").floatValue = radius;
            serialized.FindProperty("<Interval>k__BackingField").floatValue = interval;
            serialized.FindProperty("<PercentPerTick>k__BackingField").floatValue = percentPerTick;
            serialized.FindProperty("<MaxTargets>k__BackingField").intValue = maxTargets;
            serialized.FindProperty("<MaxHealersPerTarget>k__BackingField").intValue = maxHealers;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private FakeEntity AddRepairer(PlayerId owner, Vector3 position, RepairProfile profile, float hullPercentage = 1f)
        {
            FakeEntity entity = CreateEntity(owner, position, new FakeRepairable(hullPercentage));
            entity.Source = new FakeRepairSource(profile, position);
            _entities.AddEntity(entity);
            return entity;
        }

        private FakeEntity AddUnit(PlayerId owner, Vector3 position, float hullPercentage) =>
            AddEntity(owner, position, new FakeRepairable(hullPercentage));

        private FakeEntity AddEntity(PlayerId owner, Vector3 position, FakeRepairable repairable)
        {
            FakeEntity entity = CreateEntity(owner, position, repairable);
            _entities.AddEntity(entity);
            return entity;
        }

        private FakeEntity CreateEntity(PlayerId owner, Vector3 position, FakeRepairable repairable)
        {
            GameObject view = new GameObject("Repair test unit");
            _created.Add(view);
            view.transform.position = position;
            return new FakeEntity(_nextId++, owner, view.transform, repairable);
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly FakeTransform _transform;

            public FakeEntity(long id, PlayerId owner, Transform transform, FakeRepairable repairable)
            {
                Id = id;
                Owner = owner;
                _transform = new FakeTransform(transform);
                Repairable = repairable;
            }

            public long Id { get; }
            public PlayerId Owner { get; }
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public FakeRepairable Repairable { get; }
            public FakeRepairSource Source { get; set; }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (!TryGetFacade(out TFacade facade)) throw new InvalidOperationException(typeof(TFacade).Name);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                foreach (IEntityFacade candidate in new IEntityFacade[] { _transform, Repairable, Source })
                {
                    if (candidate is not TFacade match) continue;
                    facade = match;
                    return true;
                }
                facade = default;
                return false;
            }
        }

        private sealed class FakeTransform : IEntityTransformFacade
        {
            public FakeTransform(Transform transform) => Transform = transform;

            public Transform Transform { get; }
        }

        private sealed class FakeRepairable : IRepairableFacade
        {
            public FakeRepairable(float hullPercentage) => HullPercentage = hullPercentage;

            public int RepairCount { get; private set; }
            public float HullPercentage { get; private set; }
            public bool NeedsRepair => HullPercentage < 1f;

            public void Repair(float fraction)
            {
                RepairCount++;
                HullPercentage = Math.Min(1f, HullPercentage + fraction);
            }
        }

        private sealed class FakeRepairSource : IRepairSourceFacade
        {
            private readonly List<IEntity> _targets = new List<IEntity>();

            public FakeRepairSource(RepairProfile profile, Vector3 position)
            {
                Profile = profile;
                Position = position;
            }

            public event Action TargetsChanged;

            public RepairProfile Profile { get; }
            public Vector3 Position { get; }
            public bool CanRepair { get; set; } = true;
            public IReadOnlyList<IEntity> Targets => _targets;

            public void SetTargets(IReadOnlyList<IEntity> targets)
            {
                _targets.Clear();
                _targets.AddRange(targets);
                TargetsChanged?.Invoke();
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public PlayerId Owner => TestPlayers.Human;
            public bool HasShields => false;

            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
