using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    [TestFixture(typeof(IonPulseSettings), HardPointType.IonPulseCannon)]
    [TestFixture(typeof(TractorBeamSettings), HardPointType.TractorBeam)]
    [TestFixture(typeof(SensorJammingSettings), HardPointType.SensorArray)]
    [TestFixture(typeof(FighterCoordinationSettings), HardPointType.CommandArray)]
    public sealed class ShipAbilitySubsystemAvailabilityTests
    {
        private readonly Type _settingsType;
        private readonly HardPointType _requiredType;

        public ShipAbilitySubsystemAvailabilityTests(Type settingsType, HardPointType requiredType)
        {
            _settingsType = settingsType;
            _requiredType = requiredType;
        }

        [Test]
        public void MissingSubsystem_RejectsActivationAndTargeting() => AssertUnavailable();

        [Test]
        public void WrongTypeFallback_RejectsActivationAndTargeting() =>
            AssertUnavailable(CreateHardPoint(HardPointType.Weapon, false));

        [Test]
        public void DestroyedSubsystemWithLiveFallback_RejectsActivationAndTargeting() =>
            AssertUnavailable(CreateHardPoint(_requiredType, true),
                CreateHardPoint(HardPointType.Weapon, false));

        [Test]
        public void SurvivingSubsystem_AllowsPressAndTargeting()
        {
            TestCaster caster = CreateCaster(CreateHardPoint(_requiredType, true),
                CreateHardPoint(_requiredType, false));
            Assert.That(caster.Slots[0].IsAvailable, Is.True);
            Assert.That(caster.Slots[0].CanActivate, Is.True);
            Assert.That(caster.Slots[0].CanPress, Is.True);
            ShipAbilityService service = new ShipAbilityService(null, null, new VisionService(null, fogEnabled: false));
            service.Press(new IEntity[] { caster }, caster.Slots[0].Id);
            Assert.That(service.IsWaitingForTarget, Is.True);
        }

        private void AssertUnavailable(params HardPointModel[] hardPoints)
        {
            TestCaster caster = CreateCaster(hardPoints);
            Assert.That(caster.Slots[0].IsAvailable, Is.False);
            Assert.That(caster.Slots[0].CanActivate, Is.False);
            Assert.That(caster.Slots[0].CanPress, Is.False);
            ShipAbilityService service = new ShipAbilityService(null, null, new VisionService(null, fogEnabled: false));
            ShipAbilityId id = caster.Slots[0].Id;
            service.Press(new IEntity[] { caster }, id);
            Assert.That(service.IsWaitingForTarget, Is.False);
            Assert.That(service.TryActivate(caster, id, null), Is.False);
        }

        private TestCaster CreateCaster(params HardPointModel[] hardPoints)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            typeof(ShipAbilityDefinition).GetField("requiresEnemyTarget",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, true);
            typeof(ShipAbilityDefinition).GetField("settings",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, Activator.CreateInstance(_settingsType));
            // Settings own the requirement, independent of the slot ID.
            return new TestCaster(ShipAbilityId.BoostEnginePower, definition, hardPoints);
        }

        private sealed class TestCaster : IShipAbilityFacade, IEntity
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity => this;
            public IHealthModelObserver Health { get; }
            public float RadarRange => 100f;
            public long Id => 1;
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner => TestPlayers.Human;

            public TestCaster(ShipAbilityId id, ShipAbilityDefinition definition, HardPointModel[] hardPoints)
            {
                Health = new TestHealth(hardPoints);
                Slots = new[] { new ShipAbilitySlot(this, definition, id) };
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                this is TFacade facade ? facade : throw new InvalidOperationException();

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade matching) { facade = matching; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class TestHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels { get; }
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;

            public TestHealth(HardPointModel[] hardPoints) { HardPointModels = hardPoints; }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                throw new InvalidOperationException("Ability availability must not allocate targeting snapshots.");
        }

        private static HardPointModel CreateHardPoint(HardPointType type, bool destroyed)
        {
            var point = new HardPointModel(type, 0);
            point.SetHealth(destroyed ? 0f : 100f, 1f);
            return point;
        }
    }
}
