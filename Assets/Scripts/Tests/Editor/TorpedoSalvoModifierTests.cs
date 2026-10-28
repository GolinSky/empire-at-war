using EmpireAtWar.Components.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class TorpedoSalvoModifierTests
    {
        [Test]
        public void TorpedoSalvo_OnlyChangesTorpedoRate_AndResetRestoresIt()
        {
            var modifiers = new CombatModifiers();
            modifiers.SetTorpedoSalvo(2f / 3f);

            Assert.That(1f / modifiers.GetTorpedoFireDelayMultiplier(true), Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(modifiers.GetTorpedoFireDelayMultiplier(false), Is.EqualTo(1f));
            Assert.That(modifiers.HasTemporaryEffects, Is.True);

            modifiers.SetTorpedoSalvo(1f);

            Assert.That(modifiers.GetTorpedoFireDelayMultiplier(true), Is.EqualTo(1f));
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
        }
    }
}
