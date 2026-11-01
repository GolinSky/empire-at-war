using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyTaskForceSelectionModelTests
    {
        [Test]
        public void Select_CommitsUsefulShipsEvenWhenOneCanQuicklyClearTarget()
        {
            ForceComposition weak = Force(100f, (ShipClass.Capital, 1f));
            ForceComposition strong = Force(100f, (ShipClass.Capital, 100f));
            var selected = new List<int>();

            bool ready = new EnemyTaskForceSelectionModel().Select(new[] { weak, strong, strong },
                new ForceComposition(), ShipClass.Capital, 1000f, 0f, 1.1f, Array.Empty<int>(), 3, selected);

            Assert.That(ready, Is.True);
            Assert.That(selected, Is.EqualTo(new[] { 1, 2, 0 }));
        }

        [Test]
        public void Select_RetainsViableAssignmentAndAddsUsefulReinforcement()
        {
            var selected = new List<int>();
            bool ready = new EnemyTaskForceSelectionModel().Select(
                new[] { Force(100f, (ShipClass.Capital, 100f)), Force(100f, (ShipClass.Capital, 1000f)) },
                new ForceComposition(), ShipClass.Capital, 1000f, 0f, 1.1f, new[] { 0 }, 2, selected);

            Assert.That(ready, Is.True);
            Assert.That(selected, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void Select_LargeUndefendedTargetDoesNotRequireThirtySecondKill()
        {
            var selected = new List<int>();
            bool ready = new EnemyTaskForceSelectionModel().Select(new[] { Force(100f, (ShipClass.Capital, 1f)) },
                new ForceComposition(), ShipClass.Capital, 10000f, 0f, 1.1f, Array.Empty<int>(), 1, selected);

            Assert.That(ready, Is.True);
            Assert.That(selected, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Select_InsufficientForceRemainsSelectedButNotReady()
        {
            var selected = new List<int>();
            bool ready = new EnemyTaskForceSelectionModel().Select(new[] { Force(10f, (ShipClass.Capital, 1f)) },
                Force(10000f, (ShipClass.Capital, 100f)), ShipClass.Capital, 1000f, 0f, 1.1f,
                Array.Empty<int>(), 1, selected);

            Assert.That(ready, Is.False);
            Assert.That(selected, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Select_AddsDedicatedCounterToDestroyFighterDefenders()
        {
            var strike = Force(100f, (ShipClass.Capital, 100f));
            var screen = Force(100f, (ShipClass.Fighter, 100f));
            var defenders = ForceOfClass(ShipClass.Fighter, 100f, (ShipClass.Capital, 100f));
            var selected = new List<int>();

            bool ready = new EnemyTaskForceSelectionModel().Select(new[] { strike, screen }, defenders,
                ShipClass.Capital, 1000f, 0f, 1.1f, Array.Empty<int>(), 2, selected);

            Assert.That(ready, Is.True);
            Assert.That(selected, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(EnemyTaskForceSelectionModel.IsReady(screen, defenders,
                ShipClass.Capital, 1000f, 0f, 1.1f), Is.False);
        }

        [Test]
        public void Select_RejectsUselessWeaponsAndInvalidRetainedIndices()
        {
            var selected = new List<int>();
            bool ready = new EnemyTaskForceSelectionModel().Select(new[] { Force(100f, (ShipClass.Fighter, 100f)) },
                new ForceComposition(), ShipClass.Capital, 1000f, 0f, 1.1f, new[] { -1, 0, 0, 99 }, 2, selected);

            Assert.That(ready, Is.False);
            Assert.That(selected, Is.Empty);
        }

        [Test]
        public void Select_RespectsMaximumCountAndRebuildsInadequateRetainedForce()
        {
            var selected = new List<int>();
            bool ready = new EnemyTaskForceSelectionModel().Select(
                new[] { Force(10f, (ShipClass.Capital, 1f)), Force(1000f, (ShipClass.Capital, 100f)) },
                Force(100f, (ShipClass.Capital, 10f)), ShipClass.Capital, 1000f, 0f, 1.1f,
                new[] { 0 }, 1, selected);

            Assert.That(ready, Is.True);
            Assert.That(selected, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Add_PreservesCurrentHealthAndWeaponStrengthWithoutChangingSource()
        {
            var first = Force(20f, (ShipClass.Capital, 3f));
            var second = Force(40f, (ShipClass.Capital, 7f));

            first.Add(second);

            Assert.That(first.UnitCount, Is.EqualTo(2));
            Assert.That(first.Hull(ShipClass.Capital), Is.EqualTo(60f));
            Assert.That(first.HullDps(ShipClass.Capital), Is.EqualTo(10f));
            Assert.That(second.Hull(ShipClass.Capital), Is.EqualTo(40f));
            Assert.That(second.HullDps(ShipClass.Capital), Is.EqualTo(7f));
        }

        private static ForceComposition Force(float hull, params (ShipClass Target, float Dps)[] damage) =>
            ForceOfClass(ShipClass.Capital, hull, damage);

        private static ForceComposition ForceOfClass(ShipClass shipClass, float hull,
            params (ShipClass Target, float Dps)[] damage)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            foreach ((ShipClass target, float dps) in damage) rates[(int)target] = dps;
            var profile = new UnitCombatProfile(shipClass, hull, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            var force = new ForceComposition();
            force.AddNew(profile, false);
            return force;
        }
    }
}
