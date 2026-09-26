using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Health;
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

                platform.Initialize();

                Assert.That(platform.transform.position, Is.EqualTo(startPosition));
                Assert.That(radarComponent.Position, Is.EqualTo(startPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
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
            public IHealthModelObserver HealthModelObserver { get; } = new HealthModelStub();

            public void ApplyDamage(float damage, DamageType damageType, int shipUnitId)
            {
            }

            public bool Equal(IHealthModelObserver modelObserver)
            {
                return false;
            }
        }

        private sealed class HealthModelStub : IHealthModelObserver
        {
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public ShipClass ShipClass => ShipClass.Structure;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerType PlayerType => PlayerType.Player;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }

        private sealed class RadarComponentStub : IRadarComponent
        {
            public string Id => nameof(RadarComponentStub);
            public event Action<IReadOnlyList<RadarContact>> ContactsUpdated;
            public ObservableList<EmpireAtWar.Entities.BaseEntity.IEntity> Enemies => null;
            public Vector3 Position { get; private set; }

            public void SetPosition(Vector3 position)
            {
                Position = position;
            }
        }
    }
}
