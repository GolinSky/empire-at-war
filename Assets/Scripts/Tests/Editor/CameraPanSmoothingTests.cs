using EmpireAtWar.Services.Camera;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Camera
{
    public sealed class CameraPanSmoothingTests
    {
        [Test]
        public void UpdateVelocity_AcceleratesWithoutJumpingToMaximumSpeed()
        {
            Vector2 velocity = CameraPanSmoothing.UpdateVelocity(
                Vector2.zero,
                Vector2.right,
                500f,
                1000f,
                1500f,
                0.1f);

            Assert.That(velocity, Is.EqualTo(new Vector2(100f, 0f)));
        }

        [Test]
        public void UpdateVelocity_DeceleratesWithoutStoppingImmediately()
        {
            Vector2 velocity = CameraPanSmoothing.UpdateVelocity(
                new Vector2(500f, 0f),
                Vector2.zero,
                500f,
                1000f,
                1500f,
                0.1f);

            Assert.That(velocity, Is.EqualTo(new Vector2(350f, 0f)));
        }

        [Test]
        public void UpdateVelocity_NormalizesDiagonalInput()
        {
            Vector2 velocity = CameraPanSmoothing.UpdateVelocity(
                Vector2.zero,
                Vector2.one,
                500f,
                5000f,
                5000f,
                1f);

            Assert.That(velocity.magnitude, Is.EqualTo(500f).Within(0.001f));
        }
    }
}
