using EmpireAtWar.Components.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CombatModifiersTests
    {
        [Test]
        public void EmptyModifiers_AreNeutral()
        {
            CombatModifiers modifiers = new CombatModifiers();
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void StackedModifiers_UseProducts_AndRemoveRestoresValues()
        {
            CombatModifiers modifiers = new CombatModifiers();
            CombatStatModifier first = new CombatStatModifier(2f, 0.5f, 1.5f, 2f, 0.8f);
            CombatStatModifier second = new CombatStatModifier(1.25f, 0.8f, 0.5f, 1.5f, 0f);

            modifiers.Add(first);
            modifiers.Add(second);
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(2.5f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(0.4f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(0.75f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(3f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.Zero);

            modifiers.Remove(second);
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.5f));
            modifiers.Remove(first);
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }
    }
}
