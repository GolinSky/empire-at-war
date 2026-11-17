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

        [TestCase(ShipClass.Corvette, true)]
        [TestCase(ShipClass.Frigate, true)]
        [TestCase(ShipClass.Fighter, false)]
        [TestCase(ShipClass.Bomber, false)]
        [TestCase(ShipClass.Interceptor, false)]
        [TestCase(ShipClass.Cruiser, false)]
        [TestCase(ShipClass.HeavyCruiser, false)]
        [TestCase(ShipClass.Capital, false)]
        [TestCase(ShipClass.BattleCruiser, false)]
        [TestCase(ShipClass.Dreadnought, false)]
        [TestCase(ShipClass.Structure, false)]
        public void CanTarget_DefaultsToCorvettesAndFrigates(ShipClass shipClass, bool expected) =>
            Assert.That(new TractorBeamSettings().CanTarget(shipClass), Is.EqualTo(expected));
    }
}
