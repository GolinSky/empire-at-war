using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using EmpireAtWar.ViewComponents.Health;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAIBrainTests
    {
        [Test]
        public void Constructor_DependsDirectlyOnHealthModelObserver()
        {
            var constructor = typeof(ShipAIBrain).GetConstructors().Single();

            Assert.That(
                constructor.GetParameters()[1].ParameterType,
                Is.EqualTo(typeof(IHealthModelObserver)));
        }

        [Test]
        public void AssignAttackTarget_AfterHold_ReenablesAndRequestsImmediateDecision()
        {
            ShipAIBrain brain = CreateBrain();
            FakeEntity target = new FakeEntity(42, new FakeHealthModel());
            brain.Enable(false);

            brain.AssignAttackTarget(target);

            Assert.That(GetPrivateField<bool>(brain, "_isEnabled"), Is.True);
            Assert.That(GetPrivateField<float>(brain, "_decisionTimer"), Is.Zero);
            Assert.That(GetPrivateField<IEntity>(brain, "_assignedTarget"), Is.SameAs(target));

            SetPrivateField(brain, "_decisionTimer", 5f);
            brain.AssignAttackTarget(target);

            Assert.That(GetPrivateField<float>(brain, "_decisionTimer"), Is.Zero);
        }

        [Test]
        public void ClearAssignedTarget_RemovesPriorAttackOrderAndFormationOffset()
        {
            ShipAIBrain brain = CreateBrain();
            brain.AssignAttackTarget(
                new FakeEntity(42, new FakeHealthModel()),
                new Vector3(5f, 7f, 9f));

            brain.ClearAssignedTarget();

            Assert.That(GetPrivateField<IEntity>(brain, "_assignedTarget"), Is.Null);
            Assert.That(
                GetPrivateField<Vector3>(brain, "_attackFormationOffset"),
                Is.EqualTo(Vector3.zero));
        }

        private static ShipAIBrain CreateBrain()
        {
            return new ShipAIBrain(
                null,
                new FakeHealthModel(),
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static T GetPrivateField<T>(ShipAIBrain brain, string fieldName)
        {
            FieldInfo field = typeof(ShipAIBrain).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(brain);
        }

        private static void SetPrivateField<T>(
            ShipAIBrain brain,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(ShipAIBrain).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(brain, value);
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id, IHealthModelObserver healthModel)
            {
                Id = id;
                HealthModel = healthModel;
            }

            public long Id { get; }
            public EmpireAtWar.Mvc.IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType => PlayerType.Player;

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealthModel : IHealthModelObserver
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
            public FloatRange ShieldDangerStateRange => default;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform => null;
            public bool HasShields => true;

            public void InjectDependency(List<HardPointView> shipUnits)
            {
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
            {
                return Array.Empty<IHardPointModel>();
            }
        }
    }
}
