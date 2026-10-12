using EmpireAtWar.Components.Weapon;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AttackSequenceStateTests
    {
        [Test]
        public void Restart_AfterRelease_AllowsNewSequence()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.Release();
            Assert.That(sequence.TryStart(out _), Is.False);

            sequence.Restart();

            Assert.That(sequence.CurrentStatus, Is.EqualTo(AttackSequenceState.Status.Ready));
            Assert.That(sequence.TryStart(out _), Is.True);
        }

        [Test]
        public void Restart_KeepsEffectsOfReleasedSequenceUnmatched()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(out int oldGeneration);
            sequence.RegisterEffect(oldGeneration);
            sequence.Release();

            sequence.Restart();

            Assert.That(sequence.TryCompleteEffect(oldGeneration), Is.False);
            Assert.That(sequence.IsEmitting(oldGeneration), Is.False);
        }

        [Test]
        public void Restart_DoesNotInterruptRunningSequence()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(out int generation);

            sequence.Restart();

            Assert.That(sequence.IsEmitting(generation), Is.True);
        }
    }
}
