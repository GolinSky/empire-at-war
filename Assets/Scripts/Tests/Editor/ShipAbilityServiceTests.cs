using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityServiceTests
    {
        private GameObject _targetView;

        [TestCase(false, 0.2f)]
        [TestCase(true, 0.2f)]
        [TestCase(false, 0.05f)]
        [TestCase(true, 0.05f)]
        public void BeamDamage_UsesAttackerBonusesAndStopsWhenTargetCloaks(bool composite, float step)
        {
            WeaponsData weapons = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            IPhasedShipAbility beam = composite
                ? new CompositeBeamAbility(new CompositeBeamSettings(), null, weapons)
                : new ProtonBeamAbility(new ProtonBeamSettings(), null, weapons);
            FakeCommand caster = CreateCaster();
            FakeCommand target = CreateCaster();
            caster.Modifiers.Add(new CombatStatModifier(2f, 1f, 1f, 1f, 1f));
            caster.Modifiers.SetFleetCommand(true);
            caster.Modifiers.SetFocusFire(target.Entity, 1.5f);
            // Exercise the running ability without spawning its visual presentation.
            SetBeamField(beam, "_caster", caster);
            SetBeamField(beam, "_target", target.Entity);
            SetBeamField(beam, "_health", target.FakeHealth);
            SetBeamField(beam, "_damagePerSecond", 100f);
            SetBeamField(beam, "_timeLeft", 1f);

            for (int i = 0; i < Mathf.CeilToInt(1f / step) + 1; i++) beam.Advance(step);

            Assert.That(target.FakeHealth.DamageReceived, Is.EqualTo(330f).Within(0.001f));
            SetBeamField(beam, "_timeLeft", 1f);
            Assert.That(beam.IsComplete, Is.False);
            target.Modifiers.SetCloaked(true);
            Assert.That(beam.IsComplete, Is.True);
        }

        private static void SetBeamField(IPhasedShipAbility beam, string name, object value) =>
            beam.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(beam, value);

        [TearDown]
        public void TearDown()
        {
            if (_targetView != null) UnityEngine.Object.DestroyImmediate(_targetView);
        }

        [Test]
        public void Activation_ProgressesThroughDurationAndRecovery()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
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
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
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
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
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
            ShipAbilityService fixedService = new ShipAbilityService(factory, TestPlayers.CreateDuel());
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(fixedDuration.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            fixedService.LateDispose();
        }

        [Test]
        public void CasterDeath_StopsActiveAbilityOnce()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
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
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
            FakeCommand caster = CreateCaster(targeted: true);
            _targetView = new GameObject("Target");
            FakeHealth targetHealth = new FakeHealth(_targetView.transform);
            FakeEntity target = new FakeEntity(command: null, owner: TestPlayers.Enemy, health: targetHealth);

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
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
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

        [Test]
        public void UnknownAbilityId_TryActivateReturnsFalse()
        {
            FakeFactory factory = new FakeFactory();
            ShipAbilityService service = new ShipAbilityService(factory, TestPlayers.CreateDuel());
            FakeCommand caster = CreateCaster();

            Assert.That(service.TryActivate(caster, ShipAbilityId.ProtonBeam, null), Is.False);
            Assert.That(factory.Created, Is.Empty);
        }

        [Test]
        public void Save_ActiveAbility_BecomesRecoveringForRemainingPlusFullRecovery()
        {
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(), TestPlayers.CreateDuel());
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(2f);

            AbilitySlotSave save = AbilitySlotSave.From(caster.Slots[0]);

            Assert.That(save.State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(save.TimeLeft, Is.EqualTo(caster.Slots[0].TimeLeft + caster.Slots[0].Definition.RecoveryDelay));
            service.LateDispose();
        }

        [Test]
        public void Restore_LongCooldown_BarStartsFullAndCountsDownToReady()
        {
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(), TestPlayers.CreateDuel());
            FakeCommand caster = CreateCaster();
            ShipAbilitySlot slot = caster.Slots[0];
            float cooldown = slot.Definition.RecoveryDelay * 2f;

            service.Restore(slot, ShipAbilityState.Recovering, cooldown);

            Assert.That(slot.Progress01, Is.EqualTo(1f));
            service.Advance(cooldown / 2f);
            Assert.That(slot.Progress01, Is.EqualTo(0.5f).Within(0.0001f));
            service.Advance(cooldown / 2f);
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Ready));
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
        }

        [Test]
        public void Restore_ActiveState_IsRejected()
        {
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(), TestPlayers.CreateDuel());
            FakeCommand caster = CreateCaster();

            Assert.Throws<ArgumentException>(() => service.Restore(caster.Slots[0], ShipAbilityState.Active, 1f));
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

            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition,
                IEntity target) { }

            public void Stop() => StopCount++;
        }

        private sealed class FakeCommand : IShipAbilityFacade, ICombatModifiersFacade
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public FakeHealth FakeHealth { get; }
            public IHealthModelObserver Health => FakeHealth;
            public float RadarRange => 20f;

            public FakeCommand(ShipAbilityDefinition definition)
            {
                FakeHealth = new FakeHealth(null);
                Entity = new FakeEntity(command: this, owner: TestPlayers.Human, health: FakeHealth);
                Slots = new[] { new ShipAbilitySlot(id: ShipAbilityId.BoostEnginePower,
                    definition: definition, owner: this) };
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IShipAbilityFacade _command;

            public long Id => 1;
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(IShipAbilityFacade command, IHealthModelObserver health,
                PlayerId owner)
            {
                _command = command;
                Owner = owner;
                HealthModel = health;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { entityCommand = transformFacade; return true; }
                if (_command is TCommand matching)
                {
                    entityCommand = matching;
                    return true;
                }
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade, IHealthFacade
        {
            public float DamageReceived { get; private set; }
            public void ApplyDamage(float damage, DamageType damageType, int id) => DamageReceived += damage;

            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

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
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
