using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Combat;
using System.Reflection;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class TractorBeamSettingsTests
    {
        [Test]
        public void Catalog_ResolvesTypedTractorBeamPrefab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            var settings = (TractorBeamSettings)catalog.Get(ShipAbilityId.TractorBeam).Settings;

            Assert.That(settings.Beam.BeamPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(settings.Beam.BeamPrefab),
                Is.EqualTo("Assets/Prefabs/Vfx/Projectiles/TractorBeamView.prefab"));
        }

        [Test]
        public void AbilityStop_RemovesItsMovementPenalty_AndPreservesOtherModifiers()
        {
            var settings = new TractorBeamSettings();
            var ability = new TractorBeamAbility(settings);
            var modifiers = new CombatModifiers();
            var other = new CombatStatModifier(1f, 1f, 2f, 1f, 1f);
            modifiers.Add(other);
            var penalty = (CombatStatModifier)typeof(TractorBeamAbility)
                .GetField("_movementPenalty", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ability);
            CombatModifierHandle applied = modifiers.Add(penalty);
            typeof(TractorBeamAbility).GetField("_targetModifiers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ability, modifiers);
            typeof(TractorBeamAbility).GetField("_applied", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ability, applied);
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(2f * settings.SpeedMultiplier));
            ability.Stop();
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void CanTarget_OnlyShipsOfALowerClassThanTheCaster()
        {
            var settings = new TractorBeamSettings();
            for (int caster = 0; caster < HULLS_BY_SIZE.Length; caster++)
                for (int target = 0; target < HULLS_BY_SIZE.Length; target++)
                    Assert.That(settings.CanTarget(HULLS_BY_SIZE[caster], HULLS_BY_SIZE[target]),
                        Is.EqualTo(target < caster), $"{HULLS_BY_SIZE[caster]} → {HULLS_BY_SIZE[target]}");
        }

        [TestCase(ShipClass.Fighter)]
        [TestCase(ShipClass.Bomber)]
        [TestCase(ShipClass.Interceptor)]
        [TestCase(ShipClass.HeroStrikecraft)]
        [TestCase(ShipClass.Structure)]
        public void CanTarget_StrikecraftAndStructuresAreImmuneAndNeverCast(ShipClass immune)
        {
            var settings = new TractorBeamSettings();
            foreach (ShipClass other in Enum.GetValues(typeof(ShipClass)))
            {
                Assert.That(settings.CanTarget(other, immune), Is.False, $"{other} → {immune}");
                Assert.That(settings.CanTarget(immune, other), Is.False, $"{immune} → {other}");
            }
        }

        private static readonly ShipClass[] HULLS_BY_SIZE =
        {
            ShipClass.Corvette, ShipClass.Frigate, ShipClass.Cruiser, ShipClass.HeavyCruiser, ShipClass.Capital,
            ShipClass.BattleCruiser, ShipClass.Dreadnought
        };
    }
}
