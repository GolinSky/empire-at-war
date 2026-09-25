using EmpireAtWar.Services.Audio;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipEngineAudioModelTests
    {
        [Test]
        public void Cruise_EmitsOneAccelerationCue()
        {
            var model = new ShipEngineAudioModel();
            Assert.That(model.Advance(1f, 0.1f), Is.True);
            for (int i = 0; i < 50; i++)
                Assert.That(model.Advance(1f, 0.1f), Is.False);
        }

        [Test]
        public void SpeedBoost_RaisesPitchInputAndEmitsAnotherCue()
        {
            var model = new ShipEngineAudioModel();
            model.Advance(1f, 0.1f);
            model.Advance(1f, 2f);
            float cruiseSpeed = model.Speed;

            Assert.That(model.Advance(1.6f, 0.1f), Is.True);
            Assert.That(model.Speed, Is.GreaterThan(cruiseSpeed));
        }

        [Test]
        public void Stop_FadesEngineAndAllowsNextDepartureCue()
        {
            var model = new ShipEngineAudioModel();
            model.Advance(1f, 1f);
            float movingSpeed = model.Speed;

            Assert.That(model.Advance(0f, 0.1f), Is.False);
            Assert.That(model.Speed, Is.InRange(0.01f, movingSpeed));
            model.Advance(0f, 2f);
            Assert.That(model.Speed, Is.LessThan(0.002f));
            Assert.That(model.Advance(1f, 0.1f), Is.True);
        }

        [Test]
        public void RapidStopStart_DoesNotStackAccelerationCues()
        {
            var model = new ShipEngineAudioModel();
            model.Advance(1f, 0.1f);
            model.Advance(0f, 0.1f);
            Assert.That(model.Advance(1f, 0.1f), Is.False);
        }

        [Test]
        public void Pause_PreservesEngineAndAccelerationState()
        {
            var model = new ShipEngineAudioModel();
            model.Advance(1f, 0.1f);
            float speed = model.Speed;

            Assert.That(model.Advance(0f, 0f), Is.False);
            Assert.That(model.Speed, Is.EqualTo(speed));
            Assert.That(model.Advance(1f, 0.1f), Is.False);
        }
    }
}
