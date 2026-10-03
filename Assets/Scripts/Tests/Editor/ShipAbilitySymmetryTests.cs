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

        private sealed class TestCommand : IShipAbilityFacade, IAttackFacade
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; } = Array.Empty<ShipAbilitySlot>();
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 100f;
            public float NavigationRadius => 1f;

            public TestCommand(PlayerId owner, long id)
            {
                FakeHealth health = new FakeHealth();
                Health = health;
                Entity = new TestEntity(id: id, owner: owner, health: health, command: this);
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

            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
            public Transform Transform => null;
            public bool HasShields => true;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
