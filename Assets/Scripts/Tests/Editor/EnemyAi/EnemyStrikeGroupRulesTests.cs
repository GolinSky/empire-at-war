using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Hit-and-run strike ships: ranked by damage against the target, sized by readiness, minimum two.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyStrikeGroupRulesTests
    {
        private const float REQUIRED_ADVANTAGE = 1.1f;
        private static readonly ForceComposition STRONG_DEFENDERS = Force(100000f, 1000f);

        [Test]
        public void Select_RanksShipsByTheirDamageAgainstTheTarget()
        {
            List<int> selected = Select(new[] { Force(100f, 1f), Force(100f, 100f), Force(100f, 10f) },
                STRONG_DEFENDERS, maximumCount: 2);

            Assert.That(selected, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Select_AStrikeRoleBreaksATie()
        {
            ForceComposition[] candidates = { Force(100f, 10f), Force(100f, 10f), Force(100f, 10f) };
            UnitRole[] roles = { UnitRole.None, UnitRole.AntiCapital, UnitRole.None };

            List<int> selected = Select(candidates, STRONG_DEFENDERS, maximumCount: 2, roles: roles);

            Assert.That(selected, Is.EqualTo(new[] { 1, 0 }));
        }

        [Test]
        public void Select_AShipWithoutAStrikeRoleJoinsWhenItsDamageIsHigher()
        {
            ForceComposition[] candidates = { Force(100f, 100f), Force(100f, 10f), Force(100f, 1f) };
            UnitRole[] roles = { UnitRole.None, UnitRole.AntiCapital, UnitRole.Dps };

            List<int> selected = Select(candidates, STRONG_DEFENDERS, maximumCount: 2, roles: roles);

            Assert.That(selected, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void Select_StopsAtTheFirstReadySet()
        {
            ForceComposition[] candidates = { Force(100f, 10f), Force(100f, 40f), Force(100f, 30f), Force(100f, 20f) };

            List<int> selected = Select(candidates, new ForceComposition(), maximumCount: 4);

            Assert.That(selected, Is.EqualTo(new[] { 1, 2 }), "An undefended target needs only the minimum strike group.");
        }

        [Test]
        public void Select_IsCappedAtTheStrikeLimit()
        {
            ForceComposition[] candidates =
                { Force(100f, 10f), Force(100f, 10f), Force(100f, 10f), Force(100f, 10f), Force(100f, 10f) };

            List<int> selected = Select(candidates, STRONG_DEFENDERS, maximumCount: 3);

            Assert.That(selected.Count, Is.EqualTo(3));
        }

        [Test]
        public void Select_KeepsRetainedMembersBeforeBetterRankedShips()
        {
            ForceComposition[] candidates = { Force(100f, 1f), Force(100f, 100f), Force(100f, 50f) };

            List<int> selected = Select(candidates, new ForceComposition(), maximumCount: 3, retained: new[] { 0 });

            Assert.That(selected, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void Select_ARetainedMemberThatNoLongerDamagesTheTargetLeaves()
        {
            ForceComposition[] candidates = { Force(100f, 0f), Force(100f, 100f), Force(100f, 50f) };

            List<int> selected = Select(candidates, new ForceComposition(), maximumCount: 3, retained: new[] { 0 });

            Assert.That(selected, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Select_OneShipThatDamagesTheTargetIsNoStrikeGroup()
        {
            var selected = new List<int> { 7 };
            bool found = new EnemyStrikeGroupRules().Select(new[] { Force(100f, 100f), Force(100f, 0f) },
                new[] { UnitRole.None, UnitRole.None }, new ForceComposition(), ShipClass.Capital, 1000f, 0f,
                REQUIRED_ADVANTAGE, Array.Empty<int>(), 5, selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Empty);
        }

        [TestCase(ShipClass.HeavyCruiser, true)]
        [TestCase(ShipClass.Capital, true)]
        [TestCase(ShipClass.BattleCruiser, true)]
        [TestCase(ShipClass.Dreadnought, true)]
        [TestCase(ShipClass.Cruiser, false)]
        [TestCase(ShipClass.Frigate, false)]
        [TestCase(ShipClass.Corvette, false)]
        [TestCase(ShipClass.Bomber, false)]
        [TestCase(ShipClass.Structure, false)]
        public void IsStrikeTargetClass_CoversOnlyBigWarships(ShipClass shipClass, bool expected)
        {
            Assert.That(EnemyStrikeGroupRules.IsStrikeTargetClass(shipClass), Is.EqualTo(expected));
        }

        private static List<int> Select(ForceComposition[] candidates, ForceComposition defenders, int maximumCount,
            UnitRole[] roles = null, int[] retained = null)
        {
            var selected = new List<int>();
            new EnemyStrikeGroupRules().Select(candidates, roles ?? new UnitRole[candidates.Length], defenders,
                ShipClass.Capital, 1000f, 0f, REQUIRED_ADVANTAGE, retained ?? Array.Empty<int>(), maximumCount, selected);
            return selected;
        }

        private static ForceComposition Force(float hull, float capitalDps)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            rates[(int)ShipClass.Capital] = capitalDps;
            var force = new ForceComposition();
            force.AddNew(new UnitCombatProfile(ShipClass.Capital, hull, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()), false);
            return force;
        }
    }
}
