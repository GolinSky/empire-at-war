using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.UnitExplosion;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitHullScorchBlastsTests
    {
        private const int TOP_RIGHT = 0;
        private const int TOP_LEFT = 1;
        private const int BELOW = 2;

        private Transform _hull;
        private Spots _spots;
        private Pool _pool;
        private UnitHullScorchBlasts _blasts;

        [SetUp]
        public void SetUp()
        {
            _hull = new GameObject("Hull").transform;
            _spots = new Spots();
            _pool = new Pool();
            _blasts = new UnitHullScorchBlasts(_spots, _pool, null, _hull);
            _blasts.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _spots.Dispose();
            Object.DestroyImmediate(_hull.gameObject);
        }

        [Test]
        public void HullHit_PlaysOneBlastAtTheHitSpot()
        {
            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
            Assert.That(_pool.Played[0].Anchor, Is.SameAs(_spots.Anchors[TOP_LEFT]));
            Assert.That(_pool.Played[0].Scale, Is.EqualTo(_spots.Scale));
        }

        [Test]
        public void HullHit_OffsetStaysAboveTheHullAndTurnsTowardsItsMiddle()
        {
            // A fresh unit per hit, so the unit interval never skips one.
            for (int i = 0; i < 50; i++)
            {
                _blasts.LateDispose();
                _pool.Played.Clear();
                _blasts = new UnitHullScorchBlasts(_spots, _pool, null, _hull);
                _blasts.Initialize();

                _spots.Hit(TOP_RIGHT, DamageType.Turbolaser);

                Vector3 offset = _pool.Played[0].Offset;
                Assert.That(offset.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(offset.x, Is.LessThanOrEqualTo(0f), "spot sits right of the middle");
            }
        }

        [Test]
        public void PrecisionStrike_PlaysNoBlast()
        {
            _spots.Hit(TOP_LEFT, DamageType.PrecisionStrike);

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void HitDuringUnitInterval_PlaysNoBlast()
        {
            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);
            _spots.Hit(TOP_RIGHT, DamageType.Turbolaser);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
        }

        [Test]
        public void SkippedBlast_DoesNotStartTheUnitInterval()
        {
            _pool.Accepts = false;
            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);
            _pool.Accepts = true;

            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
        }

        [TestCase(BELOW)]
        [TestCase(-1)]
        public void SpotBelowHullOrUnknown_PlaysAtASpotAbove(int index)
        {
            _spots.Hit(index, DamageType.Turbolaser);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
            Assert.That(_pool.Played[0].Anchor, Is.Not.SameAs(_spots.Anchors[BELOW]));
        }

        [Test]
        public void SpotWithoutVfx_PlaysAtAnotherSpot()
        {
            _spots.EnabledIndex = TOP_RIGHT;

            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
            Assert.That(_pool.Played[0].Anchor, Is.SameAs(_spots.Anchors[TOP_RIGHT]));
        }

        [Test]
        public void LateDispose_StopsFollowingHits()
        {
            _blasts.LateDispose();

            _spots.Hit(TOP_LEFT, DamageType.Turbolaser);

            Assert.That(_pool.Played, Is.Empty);
        }

        private sealed class Pool : IHullScorchBlastPool
        {
            public List<(Transform Anchor, Vector3 Offset, float Scale)> Played { get; } =
                new List<(Transform, Vector3, float)>();

            public bool Accepts { get; set; } = true;

            public float UnitInterval => 1f;

            public bool TryPlay(Transform anchor, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float scale)
            {
                if (!Accepts) return false;
                Played.Add((anchor, worldOffset, scale));
                return true;
            }
        }

        private sealed class Spots : IHullBlastSpots, IDisposable
        {
            public event Action<int, DamageType> HullDamaged;

            public float Scale => 2f;

            /// <summary>Only this index has VFX; -1 = every spot has VFX.</summary>
            public int EnabledIndex { get; set; } = -1;

            public Transform[] Anchors { get; } =
            {
                Anchor("TopRight", new Vector3(10f, 1f, 0f)),
                Anchor("TopLeft", new Vector3(-10f, 1f, 0f)),
                Anchor("Below", new Vector3(0f, -2f, 0f)),
            };

            public int SpotCount => Anchors.Length;

            public bool TryGetSpot(int index, out Transform anchor, out float scale)
            {
                anchor = Anchors[index];
                scale = Scale;
                return EnabledIndex < 0 || EnabledIndex == index;
            }

            public void Hit(int index, DamageType damageType) => HullDamaged?.Invoke(index, damageType);

            public void Dispose()
            {
                foreach (Transform anchor in Anchors) Object.DestroyImmediate(anchor.gameObject);
            }

            private static Transform Anchor(string name, Vector3 position)
            {
                Transform anchor = new GameObject(name).transform;
                anchor.position = position;
                return anchor;
            }
        }
    }
}
