using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
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
        [TestCase(ShipAbilityId.Invulnerability)]
        [TestCase(ShipAbilityId.BoostShieldPower)]
        [TestCase(ShipAbilityId.BoostEnginePower)]
        [TestCase(ShipAbilityId.BoostWeaponPower)]
        [TestCase(ShipAbilityId.Assault)]
        public void StatAbility_StartThenStop_RestoresCasterModifiers(ShipAbilityId id)
        {
            TestCommand caster = new TestCommand(1, PlayerType.Player);
            ShipAbilityDefinition definition = CreateDefinition();
            IShipAbility ability = id switch
            {
                ShipAbilityId.Invulnerability => new InvulnerabilityAbility(),
                ShipAbilityId.BoostShieldPower => new BoostShieldPowerAbility(),
                ShipAbilityId.BoostEnginePower => new BoostEnginePowerAbility(),
                ShipAbilityId.BoostWeaponPower => new BoostWeaponPowerAbility(),
                ShipAbilityId.Assault => new AssaultAbility(),
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };

            ability.Start(caster, definition, null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            ability.Stop();
            AssertNeutral(caster.Modifiers);
        }

        [Test]
        public void ConcentrateFire_StartThenStop_RestoresAllAffectedAllies()
        {
            EntityLocator entities = new EntityLocator();
            TestCommand caster = new TestCommand(1, PlayerType.Player);
            TestCommand ally = new TestCommand(2, PlayerType.Player);
            entities.AddEntity(caster.Entity);
            entities.AddEntity(ally.Entity);
            IShipAbility ability = new ConcentrateFireAbility(entities);

            ability.Start(caster, CreateDefinition(), null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(ally.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            ability.Stop();
            AssertNeutral(caster.Modifiers);
            AssertNeutral(ally.Modifiers);
        }

        private static ShipAbilityDefinition CreateDefinition()
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            Type type = typeof(ShipAbilityDefinition);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("statModifier", flags).SetValue(definition,
                new CombatStatModifier(1.5f, 0.5f, 1.25f, 2f, 0.75f));
            type.GetField("commandRadius", flags).SetValue(definition, 100f);
            return definition;
        }

        private static void AssertNeutral(CombatModifiers modifiers)
        {
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        private sealed class TestCommand : IShipAbilityCommand, IAttackCommand
        {
            public TestCommand(long id, PlayerType playerType)
            {
                FakeHealth health = new FakeHealth();
                Health = health;
                Entity = new TestEntity(id, playerType, health, this);
            }

            public IReadOnlyList<ShipAbilitySlot> Slots { get; } = Array.Empty<ShipAbilitySlot>();
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 100f;
            public float NavigationRadius => 1f;
            public void Attack(IEntity target, Vector3 formationOffset) { }
        }

        private sealed class TestEntity : IEntity
        {
            private readonly TestCommand _command;

            public TestEntity(long id, PlayerType playerType, IHealthModelObserver health,
                TestCommand command)
            {
                Id = id;
                PlayerType = playerType;
                HealthModel = health;
                _command = command;
            }

            public long Id { get; }
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
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Armor => 1f;
            public float ArmorPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform => null;
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
