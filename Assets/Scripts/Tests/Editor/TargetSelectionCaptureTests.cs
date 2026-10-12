using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class TargetSelectionCaptureTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void DestroyObjects()
        {
            foreach (GameObject created in _objects)
                if (created != null) UnityEngine.Object.DestroyImmediate(created);
            _objects.Clear();
        }

        [Test]
        public void SharedUnit_UsesOneSlotAndReadsHealthOnce()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub unit = Unit(Vector3.forward);
                unit.Generation = 3;
                AttackData group = new AttackData(health, null, unit, null);

                int first = capture.Acquire(unit, health);
                int second = capture.Acquire(unit, health);
                Assert.That(capture.IsWreck(group), Is.False);
                capture.MarkUsed(new[] { first, second }, 2);
                capture.Refresh();

                Assert.That(second, Is.EqualTo(first));
                Assert.That(capture.GetUnit(first), Is.SameAs(unit));
                Assert.That(health.LiveReads, Is.EqualTo(1));
                Assert.That(unit.GenerationReads, Is.EqualTo(1));
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
        public void NextBatch_RefreshesGenerationAndWreckState()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub unit = Unit(Vector3.zero);
                AttackData group = new AttackData(health, null, unit, null);
                int slot = capture.Acquire(unit, health);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                capture.ClearTargets();

                unit.IsDestroyed = true;
                unit.Generation = 7;
                health.Live = false;

                Assert.That(capture.IsWreck(group), Is.True);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                Assert.That(capture.Flags[slot] & TargetSelectionCapture.SLOT_VALID, Is.Not.Zero);
                Assert.That(capture.GetGeneration(slot), Is.EqualTo(7));
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
                HardPointStub unit = Unit(Vector3.zero);
                unit.IsDestroyed = unitDestroyed;
                AttackData group = new AttackData(health, null, unit, null);
                int slot = capture.Acquire(unit, health);

                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();

                Assert.That((capture.Flags[slot] & TargetSelectionCapture.SLOT_VALID) != 0, Is.EqualTo(expected));
                Assert.That(group.CanTarget(unit), Is.EqualTo(expected));
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
                HardPointStub first = Unit(Vector3.zero);
                HardPointStub second = Unit(Vector3.up);
                int slot = capture.Acquire(first, health);
                capture.Acquire(first, health);

                capture.Release(slot);
                Assert.That(capture.GetUnit(slot), Is.SameAs(first));
                capture.Release(slot);

                Assert.That(capture.GetUnit(slot), Is.Null);
                Assert.That(capture.Acquire(second, health), Is.EqualTo(slot));
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                Assert.That((Vector3)capture.Positions[slot], Is.EqualTo(Vector3.up));
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
                HardPointStub used = Unit(Vector3.zero);
                HardPointStub unused = Unit(Vector3.zero);
                int usedSlot = capture.Acquire(used, health);
                int unusedSlot = capture.Acquire(unused, health);

                capture.MarkUsed(new[] { usedSlot, usedSlot }, 2);
                capture.Refresh();
                capture.ClearTargets();
                capture.Refresh();

                Assert.That(used.GenerationReads, Is.EqualTo(1));
                Assert.That(unused.GenerationReads, Is.Zero);
                Assert.That(capture.Flags[unusedSlot], Is.Zero);
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void Refresh_SkipsDestroyedTransformOfFreedSlot()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub dead = Unit(Vector3.zero);
                int deadSlot = capture.Acquire(dead, health);
                int reusedSlot = capture.Acquire(Unit(Vector3.zero), health);
                // Freed last, popped first: the dead slot stays free with its destroyed transform.
                capture.Release(deadSlot);
                capture.Release(reusedSlot);
                UnityEngine.Object.DestroyImmediate(dead.Transform.gameObject);
                int liveSlot = capture.Acquire(Unit(Vector3.left), health);

                capture.MarkUsed(new[] { liveSlot }, 1);
                capture.Refresh();

                Assert.That(liveSlot, Is.EqualTo(reusedSlot));
                Assert.That((Vector3)capture.Positions[liveSlot], Is.EqualTo(Vector3.left));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void ShipSlot_ReadsTransformOncePerStripeCycle()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                HardPointStub unit = Unit(Vector3.zero);
                int slot = capture.Acquire(unit, health);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();
                Assert.That((Vector3)capture.Positions[slot], Is.EqualTo(Vector3.zero));

                unit.Transform.position = Vector3.right;
                int reads = 0;
                for (int i = 0; i < TargetSelectionCapture.SHIP_POSITION_STRIPES; i++)
                {
                    capture.ClearTargets();
                    capture.MarkUsed(new[] { slot }, 1);
                    capture.Refresh();
                    if ((Vector3)capture.Positions[slot] == Vector3.right) reads++;
                    NativeArray<float3> positions = capture.Positions;
                    positions[slot] = float3.zero;
                }

                Assert.That(reads, Is.EqualTo(1));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void NewShipSlot_IsReadOnItsFirstBatch()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub();
                int first = capture.Acquire(Unit(Vector3.zero), health);
                for (int i = 0; i < 2; i++)
                {
                    capture.MarkUsed(new[] { first }, 1);
                    capture.Refresh();
                    capture.ClearTargets();
                }

                // The next stripe is 2, so the job skips slot 1; only the first-read pass sets it.
                int slot = capture.Acquire(Unit(Vector3.up), health);
                capture.MarkUsed(new[] { slot }, 1);
                capture.Refresh();

                Assert.That((Vector3)capture.Positions[slot], Is.EqualTo(Vector3.up));
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void FighterSlot_ReadsFlightPoseEveryBatch()
        {
            TargetSelectionCapture capture = new TargetSelectionCapture();
            try
            {
                HealthStub health = new HealthStub { ShipClass = ShipClass.Fighter };
                HardPointStub unit = Unit(Vector3.zero);
                unit.FlightPose = Vector3.left;
                int slot = capture.Acquire(unit, health);

                for (int i = 0; i < TargetSelectionCapture.SHIP_POSITION_STRIPES; i++)
                {
                    unit.FlightPose = Vector3.left * (i + 1);
                    capture.ClearTargets();
                    capture.MarkUsed(new[] { slot }, 1);
                    capture.Refresh();
                    Assert.That((Vector3)capture.Positions[slot], Is.EqualTo(Vector3.left * (i + 1)));
                }
            }
            finally
            {
                capture.Dispose();
            }
        }

        [Test]
        public void CanAcquire_SharedTarget_SeparatesFogPolicyAndRefreshesEachBatch()
        {
            using TargetSelectionCapture capture = new TargetSelectionCapture();
            HealthStub health = new HealthStub();
            CombatModifiers modifiers = new CombatModifiers();
            FogStub fog = new FogStub { Hidden = true };
            Entity entity = new Entity(health, null,
                new IEntityFacade[] { new CombatModifiersFacade(modifiers), fog }, default, 1);
            HardPointStub unit = Unit(Vector3.zero);
            AttackData first = new AttackData(health, null, unit, entity);
            AttackData second = new AttackData(health, null, unit, entity);

            Assert.That(capture.CanAcquire(first, false, out bool wreck), Is.True);
            Assert.That(wreck, Is.False);
            Assert.That(fog.Reads, Is.Zero);
            Assert.That(capture.CanAcquire(second, true, out wreck), Is.False);
            Assert.That(capture.CanAcquire(first, true, out wreck), Is.False);
            Assert.That(health.LiveReads, Is.EqualTo(1));
            Assert.That(fog.Reads, Is.EqualTo(1));

            health.Live = false;
            fog.Hidden = false;
            capture.ClearTargets();
            Assert.That(capture.CanAcquire(second, true, out wreck), Is.True);
            Assert.That(wreck, Is.True);
            Assert.That(health.LiveReads, Is.EqualTo(2));
            Assert.That(fog.Reads, Is.EqualTo(2));

            modifiers.SetCloaked(true);
            health.IsDestroyed = true;
            capture.ClearTargets();
            Assert.That(capture.CanAcquire(first, false, out wreck), Is.False);
            Assert.That(wreck, Is.False);

            modifiers.SetCloaked(false);
            health.IsDestroyed = false;
            health.Live = true;
            capture.ClearTargets();
            Assert.That(capture.CanAcquire(first, true, out wreck), Is.True);
            Assert.That(wreck, Is.False);
        }

        private sealed class FogStub : IFogVisibilityFacade
        {
            public bool Hidden { get; set; }
            public int Reads { get; private set; }
            public bool IsHiddenByFog { get { Reads++; return Hidden; } }
        }

        private HardPointStub Unit(Vector3 position)
        {
            GameObject unitObject = new GameObject("Unit");
            _objects.Add(unitObject);
            unitObject.transform.position = position;
            return new HardPointStub(unitObject.transform);
        }

        private sealed class HardPointStub : IHardPointModel
        {
            private int _generation;

            public HardPointStub(Transform transform) => Transform = transform;

            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Weapon;
            public float HealthPercentage => IsDestroyed ? 0f : 1f;
            public int Id => 0;
            public int GenerationReads { get; private set; }
            public int Generation
            {
                get { GenerationReads++; return _generation; }
                set => _generation = value;
            }
            public bool IsDestroyed { get; set; }
            /// <summary>Set for a fighter: its flight-model pose, which differs from the transform here.</summary>
            public Vector3? FlightPose { get; set; }
            public Vector3 Position => FlightPose ?? Transform.position;
            public Transform Transform { get; }
            public Transform Pivot => Transform;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
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
