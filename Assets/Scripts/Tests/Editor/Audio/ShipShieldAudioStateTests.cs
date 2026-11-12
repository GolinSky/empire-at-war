using EmpireAtWar.Components.Ship.Audio;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    public sealed class ShipShieldAudioStateTests
    {
        [Test]
        public void EmptySpawnDoesNotAnnounceShieldState()
        {
            var state = new ShipShieldAudioState();
            state.Reset(0f);
            Assert.That(state.Advance(0f, 0f, 0f, 0.1f, 4f), Is.Zero);
        }

        [Test]
        public void RecoveryRequiresThresholdAndEachCueHasItsOwnCooldown()
        {
            var state = new ShipShieldAudioState();
            state.Reset(100f);
            Assert.That(state.Advance(0f, 0f, 0f, 0.1f, 4f), Is.EqualTo(-1));
            Assert.That(state.Advance(9f, 0.09f, 1f, 0.1f, 4f), Is.Zero);
            Assert.That(state.Advance(10f, 0.1f, 1f, 0.1f, 4f), Is.EqualTo(1));
            Assert.That(state.Advance(0f, 0f, 2f, 0.1f, 4f), Is.Zero);
            Assert.That(state.Advance(10f, 0.1f, 3f, 0.1f, 4f), Is.Zero);
            Assert.That(state.Advance(0f, 0f, 4f, 0.1f, 4f), Is.EqualTo(-1));
            Assert.That(state.Advance(10f, 0.1f, 5f, 0.1f, 4f), Is.EqualTo(1));
        }

        [Test]
        public void RestoredValuesBecomeBaselineWithoutCues()
        {
            var state = new ShipShieldAudioState();
            state.Reset(100f);
            state.Reset(0f);
            Assert.That(state.Advance(0f, 0f, 0f, 0.1f, 4f), Is.Zero);
            state.Reset(100f);
            Assert.That(state.Advance(100f, 1f, 0f, 0.1f, 4f), Is.Zero);
        }
    }
}
