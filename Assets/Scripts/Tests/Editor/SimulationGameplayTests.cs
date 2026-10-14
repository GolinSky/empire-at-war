using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SimulationGameplayTests
    {
        private static SimulationFleetEntry Entry(int quantity = 1) => new SimulationFleetEntry(UnitTypeId.Ship(ShipType.Venator), quantity);
        private static SimulationSetup Setup(float limit = 30) => new SimulationSetup(FactionType.Republic, FactionType.Separatist,
            new[] { Entry() }, new[] { Entry() }, 2000, limit, false);
        [TestCase(0)]
        [TestCase(101)]
        public void QuantitiesOutsideOneToHundred_AreRejected(int count) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Entry(count));
        [TestCase(1)]
        [TestCase(100)]
        public void QuantityBoundaries_AreAccepted(int count) => Assert.That(Entry(count).Quantity, Is.EqualTo(count));
        [Test]
        public void Setup_RequiresBothFleets() => Assert.Throws<ArgumentException>(() => new SimulationSetup(
            FactionType.Republic, FactionType.Separatist, Array.Empty<SimulationFleetEntry>(), new[] { Entry() }, 2000, 30, false));
        [Test]
        public void Preset_HasTwoAiSidesAndNoFeatures()
        {
            var battle = SimulationPreset.Create(Setup()).Build();
            Assert.That(battle.Features, Is.EqualTo(BattleFeatures.None));
            Assert.That(battle.Map.Type, Is.EqualTo(MapType.Empty));
            Assert.That(battle.Players.Count, Is.EqualTo(2));
            foreach (var player in battle.Players)
            {
                Assert.That(player.Role, Is.EqualTo(PlayerRole.SimulationSide));
                Assert.That(player.OwnsStation || player.CanProduce, Is.False);
            }
        }
        [Test]
        public void SpawnBarrier_PreventsPrematureWin_ThenTracksResultAndReset()
        {
            var model = new SimulationModel(Setup());
            model.Advance(5, 0, 0, 0, 0);
            Assert.That(model.Result, Is.Null); Assert.That(model.Elapsed, Is.Zero);
            model.CompleteSpawning(); model.Advance(2, 2, 0, 123, 0);
            Assert.That(model.Result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(model.Result.HullA, Is.EqualTo(123));
            model.Reset(Setup());
            Assert.That(model.Result, Is.Null); Assert.That(model.Elapsed, Is.Zero); Assert.That(model.IsSpawning, Is.True);
            model.CompleteSpawning(); model.Advance(30, 2, 2, 100, 80);
            Assert.That(model.Result.Outcome, Is.EqualTo(BattleOutcome.Draw));
        }
        [Test]
        public void Targeting_AttacksNearestEnemy_AndRetargetsOnlyWhenItDies()
        {
            var roots = new List<GameObject>();
            try
            {
                Unit Make(long id, float x) { var unit = new Unit(id, x); roots.Add(unit.Root); return unit; }
                Unit left = Make(1, -100), right = Make(2, 100), nearLeft = Make(3, -90), nearRight = Make(4, 90);
                var orders = new AttackRecorder();
                var targeting = new SimulationTargeting(orders);
                targeting.Command(new IEntity[] { left, right }, new IEntity[] { nearLeft, nearRight });
                Assert.That(orders.Targets, Is.EquivalentTo(new Dictionary<long, long> { [1] = 3, [2] = 4 }));
                orders.Targets.Clear();
                targeting.Command(new IEntity[] { left, right }, new IEntity[] { nearLeft, nearRight });
                Assert.That(orders.Targets, Is.Empty);
                targeting.Command(new IEntity[] { left, right }, new IEntity[] { nearRight });
                Assert.That(orders.Targets, Is.EquivalentTo(new Dictionary<long, long> { [1] = 4 }));
            }
            finally { foreach (GameObject root in roots) UnityEngine.Object.DestroyImmediate(root); }
        }

        private sealed class Unit : IEntity, IEntityTransformFacade
        {
            public GameObject Root { get; } = new GameObject(nameof(Unit));
            public long Id { get; }
            public PlayerId Owner => default;
            public IHealthModelObserver HealthModel => null;
            public Transform Transform => Root.transform;
            public Unit(long id, float x) { Id = id; Root.transform.position = new Vector3(x, 0, 0); }
            public T GetFacade<T>() where T : IEntityFacade => (T)(object)this;
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                facade = this is T result ? result : default;
                return this is T;
            }
        }

        private sealed class AttackRecorder : IUnitOrderService
        {
            public readonly Dictionary<long, long> Targets = new();
            public event Action<UnitOrder> OrderIssued { add { } remove { } }
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target)
            {
                foreach (IEntity receiver in receivers) Targets.Add(receiver.Id, target.Id);
            }
            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) => throw new NotSupportedException();
            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) => throw new NotSupportedException();
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) => throw new NotSupportedException();
            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId) => throw new NotSupportedException();
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) => throw new NotSupportedException();
            public void IssueStop(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) => throw new NotSupportedException();
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets) => throw new NotSupportedException();
            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) => throw new NotSupportedException();
            public void IssueHunt(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
            public void IssueRetreat(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
        }

        [Test]
        public void SimulationPrefabs_HaveAllSerializedReferences()
        {
            foreach (string path in new[] { "Assets/Prefabs/Ui/MainMenu/SimulationSetupUi.prefab", "Assets/Prefabs/Ui/CoreGame/SimulationHudUi.prefab", "Assets/Prefabs/Ui/CoreGame/SurvivalHudUi.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = root.GetComponent<EmpireAtWar.Ui.Base.BaseUi>();
                    var serialized = new SerializedObject(view);
                    var field = serialized.GetIterator();
                    while (field.NextVisible(true))
                        if (field.propertyType == SerializedPropertyType.ObjectReference)
                            Assert.That(field.objectReferenceValue, Is.Not.Null, path + ": " + field.propertyPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
    }
}
