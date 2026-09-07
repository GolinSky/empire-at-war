using EmpireAtWar.Models.FogOfWar;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FogVisibilityModelTests
    {
        [Test]
        public void CalculateSoftVisibility_AtVisionRadius_IsHalfVisible()
        {
            float visibility = FogVisibilityModel.CalculateSoftVisibility(
                10f,
                10f,
                0.25f);

            Assert.That(visibility, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void CalculateSoftVisibility_InsideFeather_IsFullyVisible()
        {
            float visibility = FogVisibilityModel.CalculateSoftVisibility(
                7.5f,
                10f,
                0.25f);

            Assert.That(visibility, Is.EqualTo(1f));
        }

        [Test]
        public void CalculateSoftVisibility_OutsideFeather_IsFullyFogged()
        {
            float visibility = FogVisibilityModel.CalculateSoftVisibility(
                12.5f,
                10f,
                0.25f);

            Assert.That(visibility, Is.EqualTo(0f));
        }

        [Test]
        public void CalculateSoftVisibility_WithoutFeather_UsesHardEdge()
        {
            Assert.That(
                FogVisibilityModel.CalculateSoftVisibility(10f, 10f, 0f),
                Is.EqualTo(1f));
            Assert.That(
                FogVisibilityModel.CalculateSoftVisibility(10.01f, 10f, 0f),
                Is.EqualTo(0f));
        }
    }
}
