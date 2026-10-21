using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilitySymmetryTests
    {
        private static readonly CombatStatModifier _testModifier =
            new CombatStatModifier(1.5f, 0.5f, 1.25f, 2f, 0.75f);

        [TestCase(ShipAbilityId.Invulnerability)]
        [TestCase(ShipAbilityId.BoostShieldPower)]
        [TestCase(ShipAbilityId.BoostEnginePower)]
        [TestCase(ShipAbilityId.BoostWeaponPower)]
        [TestCase(ShipAbilityId.Assault)]
        public void StatAbility_StartThenStop_RestoresCasterModifiers(ShipAbilityId id)
        {
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            IShipAbility ability = id switch
            {
                ShipAbilityId.Invulnerability => new InvulnerabilityAbility(
                    CreateSettings<InvulnerabilitySettings>("statModifier", _testModifier)),
                ShipAbilityId.BoostShieldPower => new BoostShieldPowerAbility(
                    CreateSettings<BoostShieldPowerSettings>("statModifier", _testModifier)),
                ShipAbilityId.BoostEnginePower => new BoostEnginePowerAbility(
                    CreateSettings<BoostEnginePowerSettings>("statModifier", _testModifier)),
                ShipAbilityId.BoostWeaponPower => new BoostWeaponPowerAbility(
                    CreateSettings<BoostWeaponPowerSettings>("statModifier", _testModifier)),
                ShipAbilityId.Assault => new AssaultAbility(
                    CreateSettings<AssaultSettings>("statModifier", _testModifier)),
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            ability.Stop();
            AssertNeutral(caster.Modifiers);
        }

        [Test]
        public void ConcentrateFire_StartThenStop_RestoresAllAffectedAllies()
        {
            EntityLocator entities = new EntityLocator();
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            TestCommand ally = new TestCommand(id: 2, owner: TestPlayers.Human);
            entities.AddEntity(caster.Entity);
            entities.AddEntity(ally.Entity);
            ConcentrateFireSettings settings = CreateSettings<ConcentrateFireSettings>(
                "allyStatModifier", _testModifier);
            SetField(settings, "commandRadius", 100f);
            IShipAbility ability = new ConcentrateFireAbility(settings: settings, entities: entities);

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(ally.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            ability.Stop();
            AssertNeutral(caster.Modifiers);
            AssertNeutral(ally.Modifiers);
        }

        [Test]
        public void SensorJamming_CasterIonDisabled_Completes()
        {
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            IPhasedShipAbility ability = new SensorJammingAbility(new EntityLocator(), relations: null);

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(ability.IsComplete, Is.False);
            caster.Modifiers.SetIonDisabled(true);
            Assert.That(ability.IsComplete, Is.True);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SensorJamming_IonDisable_CleansUpAndPreservesOtherEffects(bool overlappingJammer, bool ionPulse)
        {
            EntityLocator entities = new EntityLocator();
            IPlayerRelations relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new SensorJammingFactory(entities, relations), relations);
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "duration", 30f);
            SetField(definition, "recoveryDelay", 5f);
            SetField(definition, "range", 100f);
            TestCommand first = new TestCommand(TestPlayers.Human, 1, definition);
            TestCommand second = new TestCommand(TestPlayers.Human, 2, definition);
            GameObject targetView = new GameObject("Jammed target");
            TestCommand target = new TestCommand(TestPlayers.Enemy, 3, transform: targetView.transform);
            entities.AddEntity(first.Entity);
            entities.AddEntity(second.Entity);
            entities.AddEntity(target.Entity);
            target.Modifiers.AddFighterCoordination();
            try
            {
                Assert.That(service.TryActivate(first, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                if (overlappingJammer)
                    Assert.That(service.TryActivate(second, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(0.96f).Within(0.0001f));

                if (ionPulse) first.Modifiers.AddIonPulseDisable();
                else first.Modifiers.SetIonDisabled(true);
                service.Advance(0.1f);
                Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                Assert.That(first.Slots[0].TimeLeft, Is.EqualTo(5f));
                Assert.That(target.Modifiers.AccuracyMultiplier,
                    Is.EqualTo(overlappingJammer ? 0.96f : 1.2f).Within(0.0001f));

                if (overlappingJammer)
                {
                    Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
                    second.Modifiers.SetIonDisabled(true);
                    service.Advance(0.1f);
                    Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                }
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
                Assert.That(target.Modifiers.SpeedMultiplier, Is.EqualTo(1.15f));

                if (ionPulse) first.Modifiers.RemoveIonPulseDisable();
                else first.Modifiers.SetIonDisabled(false);
                service.Advance(5f);
                Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
                Assert.That(service.TryActivate(first, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(0.96f).Within(0.0001f));
                service.Clear();
                service.LateDispose();
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            }
            finally
            {
                service.Clear();
                target.Modifiers.RemoveFighterCoordination();
                UnityEngine.Object.DestroyImmediate(targetView);
            }
            Assert.That(target.Modifiers.HasTemporaryEffects, Is.False);
        }

        private sealed class SensorJammingFactory : IShipAbilityFactory
        {
            private readonly IEntityLocator _entities;
            private readonly IPlayerRelations _relations;

            public SensorJammingFactory(IEntityLocator entities, IPlayerRelations relations)
            {
                _entities = entities;
                _relations = relations;
            }

            public IShipAbility Create(ShipAbilityDefinition definition) => new SensorJammingAbility(_entities, _relations);
        }

        private static TSettings CreateSettings<TSettings>(string modifierField, CombatStatModifier modifier)
            where TSettings : ShipAbilitySettings, new()
        {
            TSettings settings = new TSettings();
            SetField(settings, modifierField, modifier);
            return settings;
        }

        private static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(instance, value);
        }

        private static void AssertNeutral(CombatModifiers modifiers)
        {
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        private sealed class TestCommand : IShipAbilityFacade, IAttackFacade, ICombatModifiersFacade
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; } = Array.Empty<ShipAbilitySlot>();
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 100f;
            public float NavigationRadius => 1f;

            public TestCommand(PlayerId owner, long id, ShipAbilityDefinition definition = null, Transform transform = null)
            {
                FakeHealth health = new FakeHealth { Transform = transform };
                Health = health;
                Entity = new TestEntity(id: id, owner: owner, health: health, command: this);
                if (definition != null)
                    Slots = new[] { new ShipAbilitySlot(this, definition, ShipAbilityId.DauntlessSensorJamming) };
            }

            public void Attack(IEntity target, Vector3 formationOffset) { }
        }

        private sealed class TestEntity : IEntity
        {
            private readonly TestCommand _command;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public TestEntity(IHealthModelObserver health, TestCommand command, PlayerId owner,
                long id)
            {
                Id = id;
                Owner = owner;
                HealthModel = health;
                _command = command;
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

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public HardPointModel[] HardPointModels { get; } = new[]
                { new HardPointModel(HardPointType.SensorArray, 0) };
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; set; }
            public bool HasShields => true;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                hardPointType == HardPointType.SensorArray
                    ? new IHardPointModel[] { new SensorArrayStub() }
                    : Array.Empty<IHardPointModel>();
        }

        private sealed class SensorArrayStub : IHardPointModel
        {
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }

            public HardPointType HardPointType => HardPointType.SensorArray;
            public float HealthPercentage => 1f;
            public int Id => 0;
            public int Generation => 0;
            public bool IsDestroyed => false;
            public Vector3 Position => Vector3.zero;
            public Transform Transform => null;
            public Transform Pivot => null;
        }
    }
}
