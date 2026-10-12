using EmpireAtWar.Components.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FighterCoordinationModifierTests
    {
        [Test]
        public void OverlappingCarriers_KeepOneBonusUntilBothEffectsEnd()
        {
            var modifiers = new CombatModifiers();
            modifiers.AddFighterCoordination();
            modifiers.AddFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.15f));
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.HasTemporaryEffects, Is.True);
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
        }

        [Test]
        public void Coordination_ComposesWithOtherSpeedModifiers()
        {
            var modifiers = new CombatModifiers();
            var speed = new CombatStatModifier(1f, 1f, 1.3f, 1f, 1f);
            modifiers.Add(speed);
            modifiers.AddFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.3f * 1.15f).Within(.0001f));
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.3f));
        }
    }
}
