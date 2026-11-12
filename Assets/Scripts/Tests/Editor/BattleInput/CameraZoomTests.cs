using EmpireAtWar.Services.Camera;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Camera
{
    [Category("BattleInput")]
    public sealed class CameraZoomTests
    {
        private const float PITCH_SINE = 0.5f;
        private const float SMOOTH_TIME = 0.1f;
        private const float FRAME = 1f / 60f;

        private const float MIN_HEIGHT = 60f;
        private const float MAX_HEIGHT = 940f;

        [Test]
        public void Dolly_ConvertsViewDistanceToHeightByPitch()
        {
            CameraZoom zoom = new CameraZoom();
            zoom.Reset(500f);

            zoom.Dolly(300f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);

            Assert.That(zoom.TargetHeight, Is.EqualTo(650f));
        }

        [Test]
        public void Dolly_ClampsTargetToHeightRange()
        {
            CameraZoom zoom = new CameraZoom();
            zoom.Reset(500f);

            zoom.Dolly(-5000f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);
            Assert.That(zoom.TargetHeight, Is.EqualTo(60f));

            zoom.Dolly(5000f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);
            Assert.That(zoom.TargetHeight, Is.EqualTo(940f));
        }

        [Test]
        public void Step_MovesGraduallyAndSettlesOnTarget()
        {
            CameraZoom zoom = new CameraZoom();
            zoom.Reset(500f);
            zoom.Dolly(300f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);

            float height = zoom.Step(500f, SMOOTH_TIME, FRAME);
            Assert.That(height, Is.GreaterThan(500f).And.LessThan(650f));

            for (int i = 0; i < 60; i++)
            {
                height = zoom.Step(height, SMOOTH_TIME, FRAME);
            }

            Assert.That(height, Is.EqualTo(650f).Within(0.01f));
        }

        [Test]
        public void Step_RetargetsWithoutReversingMidZoom()
        {
            CameraZoom zoom = new CameraZoom();
            zoom.Reset(500f);
            zoom.Dolly(300f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);
            float height = 500f;
            for (int i = 0; i < 3; i++)
            {
                height = zoom.Step(height, SMOOTH_TIME, FRAME);
            }

            zoom.Dolly(300f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);
            float next = zoom.Step(height, SMOOTH_TIME, FRAME);

            Assert.That(next, Is.GreaterThan(height));
        }

        [Test]
        public void Reset_DropsPendingZoom()
        {
            CameraZoom zoom = new CameraZoom();
            zoom.Reset(500f);
            zoom.Dolly(300f, PITCH_SINE, MIN_HEIGHT, MAX_HEIGHT);

            zoom.Reset(400f);

            Assert.That(zoom.Step(400f, SMOOTH_TIME, FRAME), Is.EqualTo(400f));
        }
    }
}
