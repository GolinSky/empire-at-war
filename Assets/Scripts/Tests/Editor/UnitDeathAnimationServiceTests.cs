using System;
using EmpireAtWar.Services.UnitDeathAnimation;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitDeathAnimationServiceTests
    {
        [Test]
        public void Play_WithNullTransform_Throws()
        {
            UnitDeathAnimationService service = new UnitDeathAnimationService();

            Assert.Throws<ArgumentNullException>(() =>
                service.Play(null, CreateData()));
        }

        [Test]
        public void Play_WithNullData_Throws()
        {
            UnitDeathAnimationService service = new UnitDeathAnimationService();
            GameObject unit = new GameObject("Unit");

            try
            {
                Assert.Throws<ArgumentNullException>(() =>
                    service.Play(unit.transform, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }

        [Test]
        public void Play_WithInvalidDuration_Throws()
        {
            UnitDeathAnimationService service = new UnitDeathAnimationService();
            GameObject unit = new GameObject("Unit");
            FakeUnitDeathAnimationData data =
                new FakeUnitDeathAnimationData(Vector3.down, Vector3.zero, 0f);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    service.Play(unit.transform, data));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }

        [Test]
        public void Play_ReplacesExistingAnimationForSameUnit()
        {
            UnitDeathAnimationService service = new UnitDeathAnimationService();
            GameObject unit = new GameObject("Unit");
            FakeUnitDeathAnimationData data = CreateData();

            try
            {
                Assert.DoesNotThrow(() =>
                {
                    service.Play(unit.transform, data);
                    service.Play(unit.transform, data);
                    service.Dispose();
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }

        private static FakeUnitDeathAnimationData CreateData()
        {
            return new FakeUnitDeathAnimationData(
                Vector3.down,
                new Vector3(10f, 20f, 30f),
                1f);
        }

        private sealed class FakeUnitDeathAnimationData : IUnitDeathAnimationData
        {
            public FakeUnitDeathAnimationData(
                Vector3 fallDownDirection,
                Vector3 fallDownRotation,
                float fallDownDuration)
            {
                FallDownDirection = fallDownDirection;
                FallDownRotation = fallDownRotation;
                FallDownDuration = fallDownDuration;
            }

            public Vector3 FallDownDirection { get; }
            public Vector3 FallDownRotation { get; }
            public float FallDownDuration { get; }
        }
    }
}
