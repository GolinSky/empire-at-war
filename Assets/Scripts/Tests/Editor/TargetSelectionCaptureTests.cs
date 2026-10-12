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
        public void SharedUnit_UsesOneSlotAndReadsHealthAndPositionOnce()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub unit = new HardPointStub { CurrentPosition = Vector3.forward, Generation = 3 };
                AttackData group = new AttackData(health, null, unit, null);

                int first = capture.Acquire(unit, health);
                int second = capture.Acquire(unit, health);
                Assert.That(capture.IsWreck(group), Is.False);
                capture.MarkUsed(new[] { first, second }, 2);
                capture.Refresh();

                Assert.That(second, Is.EqualTo(first));
                Assert.That(capture.GetUnit(first), Is.SameAs(unit));
                Assert.That(health.LiveReads, Is.EqualTo(1));
                Assert.That(unit.PositionReads, Is.EqualTo(1));
                Assert.That(capture.GetGeneration(first), Is.EqualTo(3));
                Assert.That((Vector3)capture.Positions[first], Is.EqualTo(Vector3.forward));
                Assert.That(capture.Flags[first] & TargetSelectionCapture.SLOT_VALID, Is.Not.Zero);
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void NextBatch_RefreshesPositionGenerationAndWreckState()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub unit = new HardPointStub();
                AttackData group = new AttackData(health, null, unit, null);
                int slot = capture.Acquire(unit, health);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                capture.ClearTargets();

                unit.IsDestroyed = true;
                unit.Generation = 7;
                unit.CurrentPosition = Vector3.right;
                health.Live = false;

                Assert.That(capture.IsWreck(group), Is.True);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                Assert.That(capture.Flags[slot] & TargetSelectionCapture.SLOT_VALID, Is.Not.Zero);
                Assert.That(capture.GetGeneration(slot), Is.EqualTo(7));
                Assert.That((Vector3)capture.Positions[slot], Is.EqualTo(Vector3.right));
                Assert.That(unit.PositionReads, Is.EqualTo(2));
                Assert.That(health.LiveReads, Is.EqualTo(2));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, true)]
        public void Eligibility_MatchesLiveAttackData(bool destroyed, bool live, bool unitDestroyed, bool expected)
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub { IsDestroyed = destroyed, Live = live };
                HardPointStub unit = new HardPointStub { IsDestroyed = unitDestroyed };
                AttackData group = new AttackData(health, null, unit, null);
                int slot = capture.Acquire(unit, health);

                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();

                Assert.That((capture.Flags[slot] & TargetSelectionCapture.SLOT_VALID) != 0, Is.EqualTo(expected));
                Assert.That(group.CanTarget(unit), Is.EqualTo(expected));
                Assert.That(unit.PositionReads, Is.EqualTo(expected ? 1 : 0));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void Release_FreesSlotOnlyAfterLastReferenceAndReusesIt()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub first = new HardPointStub();
                HardPointStub second = new HardPointStub();
                int slot = capture.Acquire(first, health);
                capture.Acquire(first, health);

                capture.Release(slot);
                Assert.That(capture.GetUnit(slot), Is.SameAs(first));
                capture.Release(slot);

                Assert.That(capture.GetUnit(slot), Is.Null);
                Assert.That(capture.Acquire(second, health), Is.EqualTo(slot));
                Assert.That(capture.Acquire(first, health), Is.Not.EqualTo(slot));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void Refresh_ReadsOnlySlotsMarkedThisBatch()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub used = new HardPointStub();
                HardPointStub unused = new HardPointStub();
                int usedSlot = capture.Acquire(used, health);
                capture.Acquire(unused, health);

                capture.MarkUsed(new[] { usedSlot, usedSlot }, 2);
                capture.Refresh();
                capture.ClearTargets();
                capture.Refresh();

                Assert.That(used.PositionReads, Is.EqualTo(1));
                Assert.That(unused.PositionReads, Is.Zero);
            }
            finally
            {
                capture.Dispose();
            }
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
