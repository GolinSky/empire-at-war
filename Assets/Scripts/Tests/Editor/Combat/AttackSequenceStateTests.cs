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
            Assert.That(sequence.TryStart(1, 2, out _), Is.False);

            sequence.Restart();

            Assert.That(sequence.CurrentStatus, Is.EqualTo(AttackSequenceState.Status.Ready));
            Assert.That(sequence.TryStart(1, 2, out _), Is.True);
        }

        [Test]
        public void Restart_KeepsEffectsOfReleasedSequenceUnmatched()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(1, 2, out int oldGeneration);
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
            sequence.TryStart(1, 2, out int generation);

            sequence.Restart();

            Assert.That(sequence.IsEmitting(generation), Is.True);
        }

        [Test]
        public void StopEmitting_AllowsOverlapAndOlderEffectsReleaseCapacity()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            int unmatchedBefore = AttackSequenceDiagnostics.UnmatchedCompletions;
            Assert.That(sequence.TryStart(2, 4, out int first), Is.True);
            sequence.RegisterEffect(first);
            sequence.RegisterEffect(first);
            sequence.StopEmitting(first);
            Assert.That(sequence.IsBusy, Is.False);
            Assert.That(sequence.TryStart(2, 4, out int second), Is.True);
            sequence.RegisterEffect(second);
            sequence.RegisterEffect(second);
            sequence.StopEmitting(second);

            Assert.That(sequence.TryStart(2, 4, out _), Is.False);
            Assert.That(sequence.TryCompleteEffect(first), Is.True);
            Assert.That(sequence.TryStart(2, 4, out _), Is.False);
            Assert.That(sequence.TryCompleteEffect(first), Is.True);
            Assert.That(sequence.TryStart(2, 4, out int third), Is.True);
            Assert.That(sequence.TryCompleteEffect(second), Is.True);
            Assert.That(sequence.IsEmitting(third), Is.True);
            Assert.That(AttackSequenceDiagnostics.UnmatchedCompletions, Is.EqualTo(unmatchedBefore));
            sequence.Release();
        }

        [Test]
        public void ReleaseRestart_ResetsCapacityAcrossOverlappingSalvos()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            sequence.TryStart(1, 2, out int first);
            sequence.RegisterEffect(first);
            sequence.StopEmitting(first);
            sequence.TryStart(1, 2, out int second);
            sequence.RegisterEffect(second);
            sequence.Release();
            sequence.Restart();

            Assert.That(sequence.TryStart(2, 2, out int restarted), Is.True);
            Assert.That(sequence.RegisterEffect(first), Is.False);
            Assert.That(sequence.IsEmitting(restarted), Is.True);
            sequence.Release();
        }

        [Test]
        public void CapGate_ReservesSpaceForWholeSalvo()
        {
            AttackSequenceState sequence = new AttackSequenceState();
            Assert.That(sequence.TryStart(3, 2, out _), Is.False);
            Assert.That(sequence.TryStart(2, 2, out int generation), Is.True);
            Assert.That(sequence.TryStart(1, 2, out _), Is.False);
            sequence.RegisterEffect(generation);
            sequence.StopEmitting(generation);
            Assert.That(sequence.TryStart(2, 2, out _), Is.False);
            sequence.TryCompleteEffect(generation);
            Assert.That(sequence.TryStart(2, 2, out _), Is.True);
            sequence.Release();
        }
    }
}
