using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitExplosion;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitHullScorchBlastsTests
    {
        // One scorch level of hull loss (UnitRenderStateComponent quantizes into 31 levels). Test hulls stay
        // between level boundaries so float rounding cannot change the level.
        private const float ONE_LEVEL = 1f / 31f;

        private HealthStub _health;
        private Spots _spots;
        private Pool _pool;
        private UnitHullScorchBlasts _blasts;

        [SetUp]
        public void SetUp()
        {
            _health = new HealthStub { HullPercentage = 1f };
            _spots = new Spots();
            _pool = new Pool();
            _blasts = new UnitHullScorchBlasts(new HealthComponentStub(_health), _spots, _pool, null);
        }

        [TearDown]
        public void TearDown() => _spots.Dispose();

        [Test]
        public void Initialize_PlaysNoBlastForCurrentDamage()
        {
            _health.HullPercentage = 0.5f;
            _blasts.Initialize();

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void LevelRise_PlaysOneBlastPerLevelAtAHullSpot()
        {
            _blasts.Initialize();

            SetHull(1f - ONE_LEVEL * 0.5f);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
            Assert.That(_spots.Anchors, Does.Contain(_pool.Played[0].Anchor));
            Assert.That(_pool.Played[0].Scale, Is.EqualTo(_spots.Scale));
        }

        [Test]
        public void LargeRise_PlaysAtMostThreeBlasts()
        {
            _blasts.Initialize();

            SetHull(0.5f);

            Assert.That(_pool.Played, Has.Count.EqualTo(3));
        }

        [Test]
        public void SameLevel_PlaysNoBlast()
        {
            _blasts.Initialize();
            SetHull(1f - ONE_LEVEL * 0.3f);
            _pool.Played.Clear();

            SetHull(1f - ONE_LEVEL * 0.7f);

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void Repair_PlaysNoBlast()
        {
            _health.HullPercentage = 0.5f;
            _blasts.Initialize();

            SetHull(0.8f);

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void RiseAfterRepair_PlaysAgain()
        {
            _health.HullPercentage = 0.5f;
            _blasts.Initialize();
            SetHull(0.8f);

            SetHull(0.8f - ONE_LEVEL);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
        }

        [Test]
        public void RestoredSave_PlaysNoBlast()
        {
            _blasts.Initialize();

            _health.HullPercentage = 0.25f;
            _spots.RaiseRestored();
            _health.RaiseValueChanged();

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void Death_PlaysNoBlast()
        {
            _blasts.Initialize();

            _health.IsDestroyed = true;
            SetHull(0f);

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void SpotsWithoutVfx_AreSkipped()
        {
            _spots.EnabledIndex = 2;
            _blasts.Initialize();

            SetHull(0.5f);

            Assert.That(_pool.Played, Is.Not.Empty);
            Assert.That(_pool.Played.TrueForAll(blast => blast.Anchor == _spots.Anchors[2]), Is.True);
        }

        [Test]
        public void LateDispose_StopsFollowingHealth()
        {
            _blasts.Initialize();
            _blasts.LateDispose();

            SetHull(0.5f);

            Assert.That(_pool.Played, Is.Empty);
        }

        private void SetHull(float hullPercentage)
        {
            _health.HullPercentage = hullPercentage;
            _health.RaiseValueChanged();
        }

        private sealed class Pool : IHullScorchBlastPool
        {
            public List<(Transform Anchor, float Scale)> Played { get; } = new List<(Transform, float)>();

            public void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale) =>
                Played.Add((anchor, scale));
        }

        private sealed class Spots : IHullBlastSpots, IDisposable
        {
            public event Action Restored;

            public float Scale => 2f;

            /// <summary>Only this index has VFX; -1 = every spot has VFX.</summary>
            public int EnabledIndex { get; set; } = -1;

            public Transform[] Anchors { get; } =
            {
                new GameObject("A").transform, new GameObject("B").transform, new GameObject("C").transform,
            };

            public int SpotCount => Anchors.Length;

            public bool TryGetSpot(int index, out Transform anchor, out float scale)
            {
                anchor = Anchors[index];
                scale = Scale;
                return EnabledIndex < 0 || EnabledIndex == index;
            }

            public void RaiseRestored() => Restored?.Invoke();

            public void Dispose()
            {
                foreach (Transform anchor in Anchors) Object.DestroyImmediate(anchor.gameObject);
            }
        }

        private sealed class HealthComponentStub : IHealthComponent
        {
            public HealthComponentStub(IHealthModelObserver health)
            {
                HealthModelObserver = health;
            }

            public string Id => nameof(HealthComponentStub);
            public bool Destroyed => false;
            public IHealthModelObserver HealthModelObserver { get; }

            public void ApplyDamage(float damage, DamageType damageType, int shipUnitId) { }

            public bool Equal(IHealthModelObserver modelObserver) => false;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged;

            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage { get; set; }
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();

            public void RaiseValueChanged() => OnValueChanged?.Invoke();
        }
    }
}
