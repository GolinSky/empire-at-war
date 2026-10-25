using System;
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
        public void HasTemporaryEffects_TracksRemovableEffects_ButNotFleetCommand()
        {
            CombatModifiers modifiers = new CombatModifiers();
            CombatStatModifier boost = new CombatStatModifier(2f, 1f, 1f, 1f, 1f);
            Assert.That(modifiers.HasTemporaryEffects, Is.False);

            modifiers.SetFleetCommand(true);
            Assert.That(modifiers.HasTemporaryEffects, Is.False);

            CombatModifierHandle applied = modifiers.Add(boost);
            modifiers.AddIonPulseDisable();
            modifiers.SetCloaked(true);
            Assert.That(modifiers.HasTemporaryEffects, Is.True);

            modifiers.Remove(applied);
            modifiers.RemoveIonPulseDisable();
            Assert.That(modifiers.HasTemporaryEffects, Is.True);
            modifiers.SetCloaked(false);
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
        }

        [Test]
        public void StackedModifiers_UseProducts_AndRemoveRestoresValues()
        {
            CombatModifiers modifiers = new CombatModifiers();
            CombatStatModifier first = new CombatStatModifier(2f, 0.5f, 1.5f, 2f, 0.8f);
            CombatStatModifier second = new CombatStatModifier(1.25f, 0.8f, 0.5f, 1.5f, 0f);

            CombatModifierHandle firstApplied = modifiers.Add(first);
            CombatModifierHandle secondApplied = modifiers.Add(second);
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(2.5f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(0.4f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(0.75f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(3f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.Zero);

            modifiers.Remove(secondApplied);
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.5f));
            modifiers.Remove(firstApplied);
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void IdenticalModifiers_RemoveOnlyTheirOwnApplication()
        {
            CombatModifiers modifiers = new CombatModifiers();
            CombatStatModifier boost = new CombatStatModifier(2f, 1f, 1f, 1f, 1f);
            CombatModifierHandle first = modifiers.Add(boost);
            modifiers.Add(boost);

            modifiers.Remove(first);
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(2f));
            Assert.Throws<InvalidOperationException>(() => modifiers.Remove(first));
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void SensorJammingAndFocusFire_RaiseChanged()
        {
            CombatModifiers modifiers = new CombatModifiers();
            int changes = 0;
            modifiers.Changed += () => changes++;

            modifiers.AddSensorJamming();
            modifiers.RemoveSensorJamming();
            modifiers.SetFocusFire(null, 1.5f);
            modifiers.ClearFocusFire();

            Assert.That(changes, Is.EqualTo(4));
        }

        [Test]
        public void IsFireDelayModified_TracksFireDelayMultiplier()
        {
            CombatModifiers modifiers = new CombatModifiers();
            Assert.That(modifiers.IsFireDelayModified(), Is.False);

            CombatModifierHandle applied = modifiers.Add(new CombatStatModifier(1f, 0.25f, 1f, 1f, 1f));
            Assert.That(modifiers.IsFireDelayModified(), Is.True);

            modifiers.Remove(applied);
            Assert.That(modifiers.IsFireDelayModified(), Is.False);
        }
    }
}
