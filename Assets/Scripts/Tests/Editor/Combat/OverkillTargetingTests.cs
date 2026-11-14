using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class OverkillTargetingTests
    {
        [TestCase(ShipClass.HeavyCapital, StructureKind.SpaceStation, 1.5f)]
        [TestCase(ShipClass.HeavyCapital, StructureKind.SecondStation, 1.5f)]
        [TestCase(ShipClass.HeavyCapital, StructureKind.DefendPlatform, 1.5f)]
        [TestCase(ShipClass.Capital, StructureKind.SpaceStation, 1f)]
        [TestCase(ShipClass.Structure, StructureKind.DefendPlatform, 1f)]
        [TestCase(ShipClass.HeavyCapital, StructureKind.MiningFacility, 1f)]
        [TestCase(ShipClass.HeavyCapital, null, 1f)]
        public void ApplyDamage_ScalesEachScheduledTickAndReservationOnce(ShipClass attacker, StructureKind? kind,
            float factor)
        {
            var coordinator = new CombatAttackCoordinator();
            var owner = new GameObject("Siege weapon");
            var matrix = ScriptableObject.CreateInstance<DamageMatrixData>();
            try
            {
                JsonUtility.FromJsonOverwrite(@"{""damageTypes"":[{""damageType"":2,""damage"":{""structure"":0.5}}]}", matrix);
                var health = new HealthStub { Hull = 1000f, ShipClass = ShipClass.Structure };
                var hardPoint = new HardPointStub();
                var defense = new CombatModifiers();
                defense.Add(new CombatStatModifier(1f, 1f, 1f, 1f, 0.8f));
                var facades = new List<IEntityFacade> { new CombatModifiersFacade(defense) };
                if (kind.HasValue) facades.Add(new StructureKindFacade(kind.Value));
                var target = new Entity(health, null, facades, PlayerId.None, 1, null, null);
                var modifiers = new CombatModifiers();
                modifiers.Add(new CombatStatModifier(1.2f, 1f, 1f, 1f, 1f));
                modifiers.SetFocusFire(target, 1.25f);
                var weapon = owner.AddComponent<WeaponComponent>();
                typeof(WeaponComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(weapon, new object[] { null, null, coordinator, modifiers, null, matrix, null, null, null,
                        null, PlayerId.None, null, null, null, owner.transform, new OfflineSession(PlayerId.None), null, attacker });
                coordinator.Register(weapon);
                var bias = new ShipClassValues(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.25f, 1f);
                var profile = new WeaponProfile(WeaponType.TurboLaser, "Test", WeaponFamily.Turbolaser,
                    DamageType.Turbolaser, 120f, 1, 0f, 1f, 100f, false, false, null, 100f, Color.white,
                    Vector3.one, false, true, bias, default, 0f);

                weapon.ApplyDamage(new AttackData(health, null, hardPoint, target), hardPoint, profile, 0f, 0.25f, null);

                IList impacts = GetImpacts(coordinator);
                Assert.That(impacts.Count, Is.EqualTo(3));
                // 120 × 1.25 size bias × 1.2 modifier × 1.25 focus fire / 3 ticks = 75 raw damage per tick.
                foreach (object impact in impacts)
                {
                    Assert.That(impact.GetType().GetField("Damage").GetValue(impact), Is.EqualTo(75f * factor).Within(0.001f));
                    Assert.That(impact.GetType().GetField("ExpectedLoss").GetValue(impact), Is.EqualTo(30f * factor).Within(0.001f));
                }
            }
            finally
            {
                coordinator.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(matrix);
            }
        }

        [Test]
        public void IsDoomed_TracksInFlightLossUntilImpactsLand()
        {
            var coordinator = new CombatAttackCoordinator();
            try
            {
                var health = new HealthStub { Hull = 100f };
                var unit = new HardPointStub();
                var group = new AttackData(health, null, unit, null);
                var owner = new CommittingOwner();
                coordinator.Register(owner);

                coordinator.ScheduleImpact(owner, group, unit, 1f, default, 0f, null, 100f);
                Assert.That(coordinator.IsDoomed(health), Is.False, "Exactly lethal is inside the margin.");

                coordinator.ScheduleImpact(owner, group, unit, 1f, default, 0f, null, 30f);
                Assert.That(coordinator.IsDoomed(health), Is.True);

                MakeImpactsDue(coordinator);
                coordinator.LateTick();
                Assert.That(coordinator.PendingImpacts, Is.Zero);
                Assert.That(coordinator.IsDoomed(health), Is.False);
            }
            finally
            {
                coordinator.Dispose();
            }
        }

        [Test]
        public void IsDoomed_ClearsWithPendingAttacks()
        {
            var coordinator = new CombatAttackCoordinator();
            try
            {
                var health = new HealthStub { Hull = 10f };
                var unit = new HardPointStub();
                var owner = new CommittingOwner();
                coordinator.Register(owner);
                coordinator.ScheduleImpact(owner, new AttackData(health, null, unit, null), unit, 1f, default, 0f,
                    null, 50f);
                Assert.That(coordinator.IsDoomed(health), Is.True);

                coordinator.ClearPendingAttacks();
                Assert.That(coordinator.IsDoomed(health), Is.False);
            }
            finally
            {
                coordinator.Dispose();
            }
        }

        [Test]
        public void MatchesPass_PrefersLiveTargetsOfEachPreferenceBeforeDoomedOnes()
        {
            const byte live = WeaponTargetSelectionJob.GROUP_ELIGIBLE;
            const byte doomed = WeaponTargetSelectionJob.GROUP_DOOMED;
            int ships = (int)TargetPreference.Ships;
            Assert.That(WeaponTargetSelectionJob.GetPassCount(ships, true), Is.EqualTo(4));

            Assert.That(WeaponTargetSelectionJob.MatchesPass(0, true, true, live), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(1, true, true, doomed), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(2, true, false, live), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(3, true, false, doomed), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(0, true, true, doomed), Is.False);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(1, true, false, live), Is.False);
        }

        [Test]
        public void MatchesPass_WithoutDoomedGroupsKeepsTheOriginalPasses()
        {
            const byte live = WeaponTargetSelectionJob.GROUP_ELIGIBLE;
            Assert.That(WeaponTargetSelectionJob.GetPassCount((int)TargetPreference.Ships, false), Is.EqualTo(2));
            Assert.That(WeaponTargetSelectionJob.GetPassCount((int)TargetPreference.Any, false), Is.EqualTo(1));
            Assert.That(WeaponTargetSelectionJob.MatchesPass(0, false, true, live), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(1, false, false, live), Is.True);
            Assert.That(WeaponTargetSelectionJob.MatchesPass(1, false, true, live), Is.False);
        }

        private static void MakeImpactsDue(CombatAttackCoordinator coordinator)
        {
            IList impacts = GetImpacts(coordinator);
            for (int i = 0; i < impacts.Count; i++)
            {
                object record = impacts[i];
                record.GetType().GetField("DueTime").SetValue(record, Time.time - 1f);
                record.GetType().GetField("EarliestFrame").SetValue(record, Time.frameCount);
                impacts[i] = record;
            }
        }

        private static IList GetImpacts(CombatAttackCoordinator coordinator) => (IList)typeof(CombatAttackCoordinator)
            .GetField("_impacts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);

        private sealed class CommittingOwner : IWeaponPresenter
        {
            public float GetHitChance(AttackData attackData, WeaponProfile profile) =>
                throw new InvalidOperationException();

            public void ApplyDamage(AttackData attackData, IHardPointModel unitView, WeaponProfile profile,
                float attackDelay, float damageDuration, IncomingMissile missile) =>
                throw new InvalidOperationException();

            public bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, float damage,
                DamageType damageType, int targetId) => true;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull { get; set; }
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }

        private sealed class HardPointStub : IHardPointModel
        {
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Any;
            public float HealthPercentage => 1f;
            public int Id => 0;
            public int Generation => 1;
            public bool IsDestroyed => false;
            public Vector3 Position => Vector3.zero;
            public Transform Transform => null;
            public Transform Pivot => null;
        }
    }
}
