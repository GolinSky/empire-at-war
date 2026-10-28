using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Entities.Squadrons;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class HazardServiceTests
    {
        private GameObject _root;
        private SpaceHazardData _data;
        private EntityLocator _entities;
        private HazardService _service;
        private TestEntity _ship;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(HazardServiceTests));
            _data = ScriptableObject.CreateInstance<SpaceHazardData>();
            typeof(SpaceHazardData).GetField("<DisablesShields>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_data, true);
            _entities = new EntityLocator();
            _ship = new TestEntity(_root.transform);
            _entities.AddEntity(_ship);
            _service = new HazardService(_entities, new TestNotifier());
            _service.Initialize();
            var spots = new[]
            {
                CreateBand(0f),
                CreateBand(100f)
            };
            var layout = new MapLayout(new Dictionary<PlayerId, Vector3>(), Array.Empty<ZoneSpot>(),
                Array.Empty<SiteSpot>(), Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                new Vector2Range(), Vector3.zero, spots);
            _service.UpdateState(new BattleMap(layout, null, null, null, null));
        }

        [TearDown]
        public void TearDown()
        {
            _service.LateDispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void Overlap_LeavingOneSourceDoesNotRestoreEffects()
        {
            _root.transform.position = new Vector3(50f, 0f, 0f);
            _service.RefreshMemberships();
            _root.transform.position = new Vector3(180f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.True);
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.True);
            _root.transform.position = new Vector3(230f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.False);
        }

        [Test]
        public void BoundaryJitter_EntryCueFiresOnceUntilOutsideHysteresis()
        {
            int entries = 0;
            _service.Entered += (_, __) => entries++;
            _root.transform.position = new Vector3(-99f, 0f, 0f);
            _service.RefreshMemberships();
            _root.transform.position = new Vector3(-105f, 0f, 0f);
            _service.RefreshMemberships();
            _root.transform.position = new Vector3(-105f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(entries, Is.EqualTo(1));
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.True);
        }

        [Test]
        public void Death_ReleasesImmediatelyAndDoesNotLockRespawn()
        {
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.HasTemporaryEffects, Is.False);
            _ship.Health.Destroy();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.False);
            _ship.Health.Respawn();
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.True);
            _ship.Health.Destroy();
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Membership_EntersOnlyWhenCenterCrossesBoundary(bool squadron)
        {
            if (squadron) _ship.UnitTypeId = UnitTypeId.Squadron(default(SquadronType));
            _root.transform.position = new Vector3(-108f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
            _root.transform.position = new Vector3(-99f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.True);
            _root.transform.position = new Vector3(-109f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.True);
            _root.transform.position = new Vector3(-111f, 0f, 0f);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
        }

        [Test]
        public void NewMap_ReleasesPreviousMemberships()
        {
            _service.RefreshMemberships();
            var layout = new MapLayout(new Dictionary<PlayerId, Vector3>(), Array.Empty<ZoneSpot>(),
                Array.Empty<SiteSpot>(), Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                new Vector2Range(), Vector3.zero);
            _service.UpdateState(new BattleMap(layout, null, null, null, null));
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.False);
            _service.RefreshMemberships();
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
        }

        [Test]
        public void RemovalAndDisposal_ReleaseAllSources()
        {
            _service.RefreshMemberships();
            _entities.RemoveEntity(_ship);
            Assert.That(_ship.Modifiers.AbilitiesSuppressed, Is.False);
            _entities.AddEntity(_ship);
            _service.RefreshMemberships();
            _service.LateDispose();
            Assert.That(_ship.Modifiers.IsShieldSuppressed, Is.False);
        }

        [Test]
        public void Queries_UsePlanarFootprintAndSumCosts()
        {
            Vector3 overlap = new Vector3(50f, 900f, 0f);
            Assert.That(_service.BlocksReinforcement(overlap), Is.True);
            Assert.That(_service.TryGetHazardAt(overlap, out SpaceHazardData data), Is.True);
            Assert.That(data, Is.SameAs(_data));
            Assert.That(_service.GetPathCost(overlap), Is.EqualTo(_data.PathCost * 2f));
            Assert.That(_service.BlocksReinforcement(new Vector3(500f, 0f, 0f)), Is.False);
        }

        private sealed class TestNotifier : INotifier<BattleMap>
        {
            public void AddObserver(IObserver<BattleMap> observer) { }
            public void RemoveObserver(IObserver<BattleMap> observer) { }
        }

        private HazardShape CreateBand(float x)
        {
            return TestHazards.CreateEllipse(_data, new Vector3(x, 0f, 0f), Vector3.forward, 100f, 100f);
        }

        private sealed class TestEntity : IEntity, ICombatModifiersFacade, IUnitTypeFacade,
            IEntityTransformFacade, IMoveFacade
        {
            public long Id => 1;
            public PlayerId Owner => TestPlayers.Human;
            public TestHealth Health { get; } = new TestHealth();
            public IHealthModelObserver HealthModel => Health;
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public UnitTypeId UnitTypeId { get; set; } = UnitTypeId.Ship(ShipType.Arquitens);
            public Transform Transform { get; }
            public Vector3 WorldPosition => Transform.position;
            public float NavigationRadius => 1f;
            public TestEntity(Transform transform) => Transform = transform;
            public void MoveTo(Vector2 screenPosition) => throw new NotSupportedException();
            public void MoveTo(Vector3 worldPosition) => Transform.position = worldPosition;
            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                this is TFacade facade ? facade : throw new InvalidOperationException();
            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade found) { facade = found; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class TestHealth : IHealthModelObserver
        {
            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 100f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
            public void Destroy() { IsDestroyed = true; OnDestroy?.Invoke(); }
            public void Respawn() => IsDestroyed = false;
        }
    }
}
