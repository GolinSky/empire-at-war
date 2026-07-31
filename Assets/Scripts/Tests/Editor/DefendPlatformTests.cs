using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class DefendPlatformTests
    {
        [Test]
        public void Initialize_AppliesInjectedStartPosition()
        {
            GameObject gameObject = new GameObject(nameof(DefendPlatformTests));

            try
            {
                DefendPlatform platform = gameObject.AddComponent<DefendPlatform>();
                HealthComponentStub healthComponent = new HealthComponentStub();
                RadarComponentStub radarComponent = new RadarComponentStub();
                Vector3 startPosition = new Vector3(12f, 0f, -34f);

                SetField(platform, "_healthComponent", healthComponent);
                SetField(platform, "_radarComponent", radarComponent);
                SetField(platform, "_startPosition", startPosition);
                SetField(platform, "_monoComponents", new List<IMonoComponent>());

                platform.Initialize();

                Assert.That(platform.transform.position, Is.EqualTo(startPosition));
                Assert.That(radarComponent.Position, Is.EqualTo(startPosition));
                Assert.That(healthComponent.IsMoving, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetField<T>(DefendPlatform platform, string fieldName, T value)
        {
            FieldInfo field = typeof(DefendPlatform).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(platform, value);
        }

        private sealed class HealthComponentStub : IHealthComponent
        {
            public string Id => nameof(HealthComponentStub);
            public bool Destroyed => false;
            public IHealthModelObserver HealthModelObserver => null;
            public bool IsMoving { get; private set; }

            public void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId)
            {
            }

            public bool Equal(IHealthModelObserver modelObserver)
            {
                return false;
            }

            public void SetMovementState(bool isMoving)
            {
                IsMoving = isMoving;
            }
        }

        private sealed class RadarComponentStub : IRadarComponent
        {
            public string Id => nameof(RadarComponentStub);
            public ObservableList<EmpireAtWar.Entities.BaseEntity.IEntity> Enemies => null;
            public Vector3 Position { get; private set; }

            public void SetPosition(Vector3 position)
            {
                Position = position;
            }

            public void SetMediator(IUnitMediator mediator)
            {
            }
        }
    }
}
