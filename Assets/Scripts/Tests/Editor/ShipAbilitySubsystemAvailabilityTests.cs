using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    [TestFixture(ShipAbilityId.IonPulse, HardPointType.IonPulseCannon)]
    [TestFixture(ShipAbilityId.TractorBeam, HardPointType.TractorBeam)]
    [TestFixture(ShipAbilityId.DauntlessSensorJamming, HardPointType.SensorArray)]
    [TestFixture(ShipAbilityId.FighterCoordination, HardPointType.CommandArray)]
    public sealed class ShipAbilitySubsystemAvailabilityTests
    {
        private readonly ShipAbilityId _abilityId;
        private readonly HardPointType _requiredType;

        public ShipAbilitySubsystemAvailabilityTests(ShipAbilityId abilityId, HardPointType requiredType)
        {
            _abilityId = abilityId;
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
            ShipAbilityService service = new ShipAbilityService(null, null);
            service.Press(new IEntity[] { caster }, _abilityId);
            Assert.That(service.IsWaitingForTarget, Is.True);
        }

        private void AssertUnavailable(params HardPointModel[] hardPoints)
        {
            TestCaster caster = CreateCaster(hardPoints);
            Assert.That(caster.Slots[0].IsAvailable, Is.False);
            Assert.That(caster.Slots[0].CanActivate, Is.False);
            Assert.That(caster.Slots[0].CanPress, Is.False);
            ShipAbilityService service = new ShipAbilityService(null, null);
            service.Press(new IEntity[] { caster }, _abilityId);
            Assert.That(service.IsWaitingForTarget, Is.False);
            Assert.That(service.TryActivate(caster, _abilityId, null), Is.False);
        }

        private TestCaster CreateCaster(params HardPointModel[] hardPoints)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            typeof(ShipAbilityDefinition).GetField("requiresEnemyTarget",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, true);
            return new TestCaster(_abilityId, definition, hardPoints);
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
            public HardPointModel[] HardPointModels { get; }
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
