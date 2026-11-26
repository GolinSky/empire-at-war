using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class UnitMatchupResolverTests
    {
        private MatchupRules _rules;

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_rules);

        [Test]
        public void OneRule_GivesAttackerStrongAndTargetWeak()
        {
            _rules = Rules(new[] { Rule(Classes(ShipClass.Bomber), Classes(ShipClass.Capital)) });

            Assert.That(Labels(ShipClass.Bomber, UnitRole.None, strong: true), Is.EqualTo(new[] { "Capital ships" }));
            Assert.That(Labels(ShipClass.Capital, UnitRole.None, strong: false), Is.EqualTo(new[] { "Bombers" }));
            Assert.That(Labels(ShipClass.Capital, UnitRole.None, strong: true), Is.Empty);
        }

        [Test]
        public void RoleRule_ReachesUnitsWithThatRole()
        {
            _rules = Rules(new[] { Rule(new MatchupSide(Array.Empty<ShipClass>(), UnitRole.Artillery), Classes(ShipClass.Capital)) });

            Assert.That(Labels(ShipClass.Frigate, UnitRole.Artillery | UnitRole.Slow, strong: true),
                Is.EqualTo(new[] { "Capital ships" }));
            Assert.That(Labels(ShipClass.Frigate, UnitRole.Slow, strong: true), Is.Empty);
            Assert.That(Labels(ShipClass.Capital, UnitRole.None, strong: false), Is.EqualTo(new[] { "Artillery ships" }));
        }

        [Test]
        public void ClassRuleInsideOwnGroup_IsLeftOut()
        {
            _rules = Rules(new[] { Rule(Classes(ShipClass.HeavyCruiser), Classes(ShipClass.Cruiser, ShipClass.Frigate)) });

            Assert.That(Labels(ShipClass.HeavyCruiser, UnitRole.None, strong: true), Is.EqualTo(new[] { "Frigates" }));
            Assert.That(Labels(ShipClass.Cruiser, UnitRole.None, strong: false), Is.Empty);
        }

        [Test]
        public void OwnFactionIcon_IsHidden()
        {
            _rules = Rules(new[] { Rule(Classes(ShipClass.Bomber), Classes(ShipClass.Capital)) });
            var strong = new List<TooltipIcon>();

            UnitMatchupResolver.Resolve(_rules, ShipClass.Bomber, UnitRole.None, FactionType.Empire, strong, new List<TooltipIcon>());

            Assert.That(strong.Single().IconKeys, Is.EqualTo(new[] { "Republic:Capital ships" }));
        }

        private string[] Labels(ShipClass shipClass, UnitRole roles, bool strong)
        {
            var strongRows = new List<TooltipIcon>();
            var weakRows = new List<TooltipIcon>();
            UnitMatchupResolver.Resolve(_rules, shipClass, roles, FactionType.Separatist, strongRows, weakRows);
            return (strong ? strongRows : weakRows).Select(row => row.Label).ToArray();
        }

        private static MatchupSide Classes(params ShipClass[] classes) => new MatchupSide(classes);

        private static MatchupRule Rule(MatchupSide attacker, MatchupSide target) => new MatchupRule(attacker, target);

        private static MatchupGroup Group(string label, MatchupSide members) => new MatchupGroup(label, members, new[]
        {
            new MatchupIcon(FactionType.Republic, "Republic:" + label),
            new MatchupIcon(FactionType.Empire, "Empire:" + label)
        });

        private static MatchupRules Rules(MatchupRule[] rules)
        {
            var asset = ScriptableObject.CreateInstance<MatchupRules>();
            MatchupGroup[] groups =
            {
                Group("Bombers", Classes(ShipClass.Bomber)),
                Group("Frigates", Classes(ShipClass.Frigate)),
                Group("Cruisers", Classes(ShipClass.Cruiser, ShipClass.HeavyCruiser)),
                Group("Capital ships", Classes(ShipClass.Capital)),
                Group("Artillery ships", new MatchupSide(Array.Empty<ShipClass>(), UnitRole.Artillery))
            };
            const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(MatchupRules).GetField("rules", FLAGS).SetValue(asset, rules);
            typeof(MatchupRules).GetField("groups", FLAGS).SetValue(asset, groups);
            return asset;
        }
    }
}
