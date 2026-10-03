using System;
using EmpireAtWar.Models.Players;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
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
                Vector3 startPosition = new Vector3(12f, 0f, -34f);

                SetField(platform, "_healthComponent", healthComponent);
                SetField(platform, "_startPosition", startPosition);

                platform.Initialize();

                Assert.That(platform.transform.position, Is.EqualTo(startPosition));
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
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
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
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
