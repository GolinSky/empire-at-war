using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityServiceTests
    {
        private GameObject _targetView;

        [TearDown]
        public void TearDown()
        {
            if (_targetView != null) UnityEngine.Object.DestroyImmediate(_targetView);
        }

        [Test]
        public void Activation_ProgressesThroughDurationAndRecovery()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand caster = CreateCaster();

            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.Advance(4f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.Advance(1f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
            service.Advance(6f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));

            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
            Assert.That(factory.Created.Count, Is.EqualTo(2));
            Assert.That(factory.Created[1], Is.Not.SameAs(factory.Created[0]));
            service.LateDispose();
            Assert.That(factory.Created[1].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroRecovery_ReactivatedAbility_ElapsesOncePerAdvance()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand caster = CreateCaster(recoveryDelay: 0f);

            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(5f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(1f);
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(4f));
            service.LateDispose();
            Assert.That(factory.Created[1].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void Cancel_StopsOnceAndEntersFullRecovery_OnlyWhenAllowed()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand caster = CreateCaster();
            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);

            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(6f));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
            service.Advance(6f);
            service.LateDispose();
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));

            FakeCommand fixedDuration = CreateCaster(canCancel: false);
            ShipAbilityService fixedService = new ShipAbilityService(factory);
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(fixedDuration.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            fixedService.LateDispose();
        }

        [Test]
        public void CasterDeath_StopsActiveAbilityOnce()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            caster.FakeHealth.IsDestroyedValue = true;

            service.Advance(1f);
            service.LateDispose();
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void Targeting_RejectsOutOfRangeAndActivatesInRange()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand caster = CreateCaster(targeted: true);
            _targetView = new GameObject("Target");
            FakeHealth targetHealth = new FakeHealth(_targetView.transform);
            FakeEntity target = new FakeEntity(null, PlayerType.Opponent, targetHealth);

            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(service.IsWaitingForTarget, Is.True);
            _targetView.transform.position = Vector3.right * 11f;
            service.SubmitTarget(target);
            Assert.That(service.IsWaitingForTarget, Is.True);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

            _targetView.transform.position = Vector3.right * 9f;
            service.SubmitTarget(target);
            Assert.That(service.IsWaitingForTarget, Is.False);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.LateDispose();
        }

        [Test]
        public void GroupPress_ActivatesOnlyReadySlots()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory);
            FakeCommand first = CreateCaster();
            FakeCommand second = CreateCaster();
            service.TryActivate(first, ShipAbilityId.BoostEnginePower, null);
            service.Advance(5f);

            service.Press(new IEntity[] { first.Entity, second.Entity },
                ShipAbilityId.BoostEnginePower);

            Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.LateDispose();
        }

        private static FakeCommand CreateCaster(bool canCancel = true, bool targeted = false,
            float recoveryDelay = 6f)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "duration", 5f);
            SetField(definition, "recoveryDelay", recoveryDelay);
            SetField(definition, "canCancel", canCancel);
            SetField(definition, "requiresEnemyTarget", targeted);
            SetField(definition, "range", 10f);
            return new FakeCommand(definition);
        }

        private static void SetField(object instance, string name, object value)
        {
            typeof(ShipAbilityDefinition).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }

        private sealed class FakeFactory : IShipAbilityFactory
        {
            public readonly List<RecordingAbility> Created = new List<RecordingAbility>();

            public IShipAbility Create(ShipAbilityDefinition definition)
            {
                RecordingAbility ability = new RecordingAbility();
                Created.Add(ability);
                return ability;
            }
        }

        private sealed class RecordingAbility : IShipAbility
        {
            public int StopCount { get; private set; }
            public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition,
                IEntity target) { }
            public void Stop() => StopCount++;
        }

        private sealed class FakeCommand : IShipAbilityCommand
        {
            public FakeCommand(ShipAbilityDefinition definition)
            {
                FakeHealth = new FakeHealth(null);
                Entity = new FakeEntity(this, PlayerType.Player, FakeHealth);
                Slots = new[] { new ShipAbilitySlot(ShipAbilityId.BoostEnginePower,
                    definition, this) };
            }

            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public FakeHealth FakeHealth { get; }
            public IHealthModelObserver Health => FakeHealth;
            public float RadarRange => 20f;
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IShipAbilityCommand _command;

            public FakeEntity(IShipAbilityCommand command, PlayerType playerType,
                IHealthModelObserver health)
            {
                _command = command;
                PlayerType = playerType;
                HealthModel = health;
            }

            public long Id => 1;
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType { get; }

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                if (_command is TCommand matching)
                {
                    entityCommand = matching;
                    return true;
                }
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public FakeHealth(Transform transform) { Transform = transform; }
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsDestroyedValue { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
