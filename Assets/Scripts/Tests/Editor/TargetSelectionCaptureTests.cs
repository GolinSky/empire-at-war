using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class TargetSelectionCaptureTests
    {
        [Test]
        public void SharedTarget_ReadsHealthAndPositionOnceAndPreservesOwnerGroup()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            HealthStub health = new HealthStub();
            HardPointStub unit = new HardPointStub { CurrentPosition = Vector3.forward, Generation = 3 };
            AttackData first = new AttackData(health, null, unit, null);
            AttackData second = new AttackData(health, null, unit, null);

            Assert.That(capture.IsWreck(first), Is.False);
            Assert.That(capture.TryCapture(first, unit, out var firstCandidate), Is.True);
            Assert.That(capture.TryCapture(second, unit, out var secondCandidate), Is.True);

            Assert.That(health.LiveReads, Is.EqualTo(1));
            Assert.That(unit.PositionReads, Is.EqualTo(1));
            Assert.That(firstCandidate.Group, Is.SameAs(first));
            Assert.That(secondCandidate.Group, Is.SameAs(second));
            Assert.That(secondCandidate.Generation, Is.EqualTo(3));
            Assert.That(secondCandidate.Position, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void NextCapture_RefreshesPositionGenerationAndWreckState()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            HealthStub health = new HealthStub();
            HardPointStub unit = new HardPointStub();
            AttackData group = new AttackData(health, null, unit, null);
            Assert.That(capture.TryCapture(group, unit, out _), Is.True);

            unit.IsDestroyed = true;
            unit.Generation = 7;
            unit.CurrentPosition = Vector3.right;
            health.Live = false;
            capture.Clear();

            Assert.That(capture.IsWreck(group), Is.True);
            Assert.That(capture.TryCapture(group, unit, out var candidate), Is.True);
            Assert.That(candidate.Generation, Is.EqualTo(7));
            Assert.That(candidate.Position, Is.EqualTo(Vector3.right));
            Assert.That(unit.PositionReads, Is.EqualTo(2));
            Assert.That(health.LiveReads, Is.EqualTo(2));
        }

        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, true)]
        public void Eligibility_MatchesLiveAttackData(bool destroyed, bool live, bool unitDestroyed, bool expected)
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            HealthStub health = new HealthStub { IsDestroyed = destroyed, Live = live };
            HardPointStub unit = new HardPointStub { IsDestroyed = unitDestroyed };
            AttackData group = new AttackData(health, null, unit, null);

            Assert.That(capture.TryCapture(group, unit, out _), Is.EqualTo(expected));
            Assert.That(group.CanTarget(unit), Is.EqualTo(expected));
            Assert.That(unit.PositionReads, Is.EqualTo(expected ? 1 : 0));
        }

        private sealed class HardPointStub : IHardPointModel
        {
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Weapon;
            public float HealthPercentage => IsDestroyed ? 0f : 1f;
            public int Id => 0;
            public int Generation { get; set; }
            public bool IsDestroyed { get; set; }
            public int PositionReads { get; private set; }
            public Vector3 CurrentPosition { get; set; }
            public Vector3 Position { get { PositionReads++; return CurrentPosition; } }
            public Transform Transform => throw new NotSupportedException();
            public Transform Pivot => throw new NotSupportedException();
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool Live { get; set; } = true;
            public int LiveReads { get; private set; }
            public bool HasLiveHardPoints { get { LiveReads++; return Live; } }
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
