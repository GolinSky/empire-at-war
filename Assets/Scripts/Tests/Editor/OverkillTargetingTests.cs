using System;
using System.Collections;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class OverkillTargetingTests
    {
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
            IList impacts = (IList)typeof(CombatAttackCoordinator)
                .GetField("_impacts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(coordinator);
            for (int i = 0; i < impacts.Count; i++)
            {
                object record = impacts[i];
                record.GetType().GetField("DueTime").SetValue(record, Time.time - 1f);
                record.GetType().GetField("EarliestFrame").SetValue(record, Time.frameCount);
                impacts[i] = record;
            }
        }

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
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
