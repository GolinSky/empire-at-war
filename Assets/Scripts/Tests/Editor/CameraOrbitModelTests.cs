using EmpireAtWar.Services.Camera;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Camera
{
    public sealed class CameraOrbitModelTests
    {
        [Test]
        public void Rotate_ClampsPitchAndNormalizesYaw()
        {
            CameraOrbitModel model = new CameraOrbitModel(55f, 0f, 25f, 80f);

            model.Rotate(100f, -45f);

            Assert.That(model.Pitch, Is.EqualTo(80f));
            Assert.That(model.Yaw, Is.EqualTo(315f));
        }

        [Test]
        public void Reset_RestoresDefaultOrientation()
        {
            CameraOrbitModel model = new CameraOrbitModel(55f, 10f, 25f, 80f);
            model.Rotate(-20f, 90f);

            model.Reset();

            Assert.That(model.Pitch, Is.EqualTo(55f));
            Assert.That(model.Yaw, Is.EqualTo(10f));
        }

        [Test]
        public void Constructor_RejectsInvertedPitchRange()
        {
            Assert.Throws<System.ArgumentException>(
                () => new CameraOrbitModel(55f, 0f, 80f, 25f));
        }
    }
}
