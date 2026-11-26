using System;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units.Matchups;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class MatchupRulesPreferenceTests
    {
        private const string SHIPPED_PATH = "Assets/Settings/Data/Units/MatchupRules.asset";
        private MatchupRules _rules;

        [SetUp]
        public void SetUp()
        {
            _rules = ScriptableObject.CreateInstance<MatchupRules>();
            const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(MatchupRules).GetField("rules", FLAGS).SetValue(_rules, new[]
            {
                new MatchupRule(new MatchupSide(new[] { ShipClass.Bomber }), new MatchupSide(new[] { ShipClass.Capital })),
                new MatchupRule(new MatchupSide(Array.Empty<ShipClass>(), UnitRole.Artillery),
                    new MatchupSide(new[] { ShipClass.Frigate }))
            });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_rules);

        [Test]
        public void Attacker_PrefersForceOfItsTargets() =>
            Assert.That(_rules.GetPreference(ShipClass.Bomber, Force(ShipClass.Capital)), Is.GreaterThan(1f));

        [Test]
        public void Target_AvoidsForceOfItsAttackers() =>
            Assert.That(_rules.GetPreference(ShipClass.Capital, Force(ShipClass.Bomber)), Is.LessThan(1f));

        [Test]
        public void RoleOnlyRule_DoesNotMoveClassPreference()
        {
            Assert.That(_rules.GetPreference(ShipClass.Frigate, Force(ShipClass.Capital)), Is.EqualTo(1f));
            Assert.That(_rules.GetPreference(ShipClass.Capital, Force(ShipClass.Frigate)), Is.EqualTo(1f));
        }

        [Test]
        public void ShippedRules_EverySideNamesClassesOrRoles()
        {
            var shipped = AssetDatabase.LoadAssetAtPath<MatchupRules>(SHIPPED_PATH);
            Assert.That(shipped, Is.Not.Null);
            Assert.That(shipped.Rules, Is.Not.Empty);
            foreach (MatchupRule rule in shipped.Rules)
            {
                Assert.That(rule.Attacker.Classes.Length > 0 || rule.Attacker.Roles != UnitRole.None, Is.True);
                Assert.That(rule.Target.Classes.Length > 0 || rule.Target.Roles != UnitRole.None, Is.True);
            }
        }

        private static ForceComposition Force(ShipClass shipClass)
        {
            var force = new ForceComposition();
            force.AddNew(new UnitCombatProfile(shipClass, 1000f, 0f, new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                Array.Empty<UnitCombatProfile>()), false);
            return force;
        }
    }
}
