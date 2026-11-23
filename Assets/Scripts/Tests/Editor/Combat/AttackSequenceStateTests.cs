using EmpireAtWar.Components.Weapon;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
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
        public void Restart_KeepsReleasedGenerationStopped()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(out int oldGeneration);
            sequence.Release();

            sequence.Restart();

            Assert.That(sequence.IsEmitting(oldGeneration), Is.False);
            Assert.That(sequence.TryStart(out int newGeneration), Is.True);
            Assert.That(newGeneration, Is.Not.EqualTo(oldGeneration));
        }

        [Test]
        public void Restart_DoesNotInterruptRunningSequence()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(out int generation);

            sequence.Restart();

            Assert.That(sequence.IsEmitting(generation), Is.True);
        }

        [Test]
        public void TryStart_RefusesOnlyWhileEmitting()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            Assert.That(sequence.TryStart(out int first), Is.True);
            Assert.That(sequence.TryStart(out _), Is.False, "A salvo in progress holds the mount.");
            sequence.StopEmitting(first);
            Assert.That(sequence.IsBusy, Is.False);

            // Shots of earlier salvos may still be flying; they never hold the mount.
            for (int salvo = 0; salvo < 5; salvo++)
            {
                Assert.That(sequence.TryStart(out int next), Is.True);
                sequence.StopEmitting(next);
            }
            sequence.Release();
        }

        [Test]
        public void StopEmitting_IgnoresStaleGeneration()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(out int first);
            sequence.StopEmitting(first);
            sequence.TryStart(out int second);

            sequence.StopEmitting(first);

            Assert.That(sequence.IsEmitting(second), Is.True);
            sequence.Release();
        }
    }
}
