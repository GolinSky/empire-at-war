using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class FighterCoordinationModifierTests
    {
        [TestCase(0, 100f, true)]
        [TestCase(2, 100f, true)]
        [TestCase(1, 100f, false)]
        [TestCase(2, 751f, false)]
        public void Coordination_OnlyAffectsAlliedSquadronsInRange(int owner, float distance, bool affected)
        {
            var caster = new TestUnit(1, TestPlayers.Human, Vector3.zero);
            var squadron = new TestUnit(2, new PlayerId(owner), Vector3.right * distance);
            try
            {
                var entities = new EntityLocator();
                entities.AddEntity(squadron);
                IShipAbility ability = new FighterCoordinationSettings().CreateAbility(
                    new ShipAbilityDependencies(entities, TestPlayers.CreateTeamGame(), null, null, null));

                ability.Start(caster, new ShipAbilityDefinition(), null);
                Assert.That(squadron.Modifiers.AccuracyMultiplier, Is.EqualTo(affected ? 1.2f : 1f));
                Assert.That(squadron.Modifiers.SpeedMultiplier, Is.EqualTo(affected ? 1.15f : 1f));
                ability.Stop();
                Assert.That(squadron.Modifiers.HasTemporaryEffects, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(caster.Transform.gameObject);
                UnityEngine.Object.DestroyImmediate(squadron.Transform.gameObject);
            }
        }

        [Test]
        public void Coordination_OverlappingAlliedCarriers_RemoveOnlyTheirOwnEffect()
        {
            var caster = new TestUnit(1, TestPlayers.Human, Vector3.zero);
            var ally = new TestUnit(2, TestPlayers.Ally, Vector3.zero);
            try
            {
                var entities = new EntityLocator();
                entities.AddEntity(ally);
                var dependencies = new ShipAbilityDependencies(entities, TestPlayers.CreateTeamGame(), null, null, null);
                var settings = new FighterCoordinationSettings();
                IShipAbility first = settings.CreateAbility(dependencies);
                IShipAbility second = settings.CreateAbility(dependencies);
                first.Start(caster, new ShipAbilityDefinition(), null);
                second.Start(ally, new ShipAbilityDefinition(), null);
                first.Stop();
                Assert.That(ally.Modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
                Assert.That(ally.Modifiers.SpeedMultiplier, Is.EqualTo(1.15f));
                second.Stop();
                Assert.That(ally.Modifiers.HasTemporaryEffects, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(caster.Transform.gameObject);
                UnityEngine.Object.DestroyImmediate(ally.Transform.gameObject);
            }
        }

        [Test]
        public void OverlappingCarriers_KeepOneBonusUntilBothEffectsEnd()
        {
            var modifiers = new CombatModifiers();
            modifiers.AddFighterCoordination();
            modifiers.AddFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.15f));
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.HasTemporaryEffects, Is.True);
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
        }

        [Test]
        public void Coordination_ComposesWithOtherSpeedModifiers()
        {
            var modifiers = new CombatModifiers();
            var speed = new CombatStatModifier(1f, 1f, 1.3f, 1f, 1f);
            modifiers.Add(speed);
            modifiers.AddFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.3f * 1.15f).Within(.0001f));
            modifiers.RemoveFighterCoordination();
            Assert.That(modifiers.AccuracyMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.3f));
        }

        private sealed class TestUnit : IEntity, IShipAbilityFacade, ICombatModifiersFacade,
            IUnitTypeFacade, IEntityTransformFacade, IHealthModelObserver, IHardPointModel
        {
            public long Id { get; }
            int IHardPointModel.Id => 0;
            public PlayerId Owner { get; }
            public Transform Transform { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public UnitTypeId UnitTypeId => UnitTypeId.Squadron(SquadronType.XWing);
            public IReadOnlyList<ShipAbilitySlot> Slots => Array.Empty<ShipAbilitySlot>();
            public IEntity Entity => this;
            public IHealthModelObserver Health => this;
            public IHealthModelObserver HealthModel => this;
            public Vector3 WorldPosition => Transform.position;
            public float RadarRange => 1000f;
            public HardPointType HardPointType => HardPointType.CommandArray;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public float HealthPercentage => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public bool HasUnits => true;
            public bool HasShields => true;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public int Generation => 0;
            public Vector3 Position => WorldPosition;
            public Transform Pivot => Transform;
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }

            public TestUnit(long id, PlayerId owner, Vector3 position)
            {
                Id = id;
                Owner = owner;
                Transform = new GameObject("Coordination test unit").transform;
                Transform.position = position;
            }

            public IHardPointModel[] GetShipUnits(HardPointType type) => new IHardPointModel[] { this };
            public T GetFacade<T>() where T : IEntityFacade => (T)(object)this;
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                facade = this is T matching ? matching : default;
                return this is T;
            }
        }
    }
}
