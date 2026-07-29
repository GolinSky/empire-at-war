using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyUnitCommanderTests
    {
        [Test]
        public void Initialize_AssignsDistinctFormationDestinations()
        {
            FakeShip first = new FakeShip(new Vector3(150f, 0f, -170f));
            FakeShip second = new FakeShip(new Vector3(170f, 0f, -170f));
            FakeShipService shipService = new FakeShipService(first, second);
            FakeReinforcementZonesSystem zones = new FakeReinforcementZonesSystem();
            FakeEntityLocator entities = new FakeEntityLocator();
            FakeGameModel gameModel = new FakeGameModel();
            EnemyUnitCommander commander = new EnemyUnitCommander(
                shipService,
                zones,
                entities,
                gameModel,
                new EnemyStrategicDecisionModel(),
                new EnemyStrategicContextBuilder(shipService, zones, entities, gameModel),
                new EnemyTaskForceExecutor());

            commander.Initialize();

            Assert.That(first.AssignedMoveTarget, Is.Not.EqualTo(second.AssignedMoveTarget));
            Assert.That(first.AssignedMoveTarget.x, Is.EqualTo(49f));
            Assert.That(second.AssignedMoveTarget.x, Is.EqualTo(61f));
        }

        [Test]
        public void ExecuteHuntFleet_AssignsDistinctAttackFormationOffsets()
        {
            FakeShip first = new FakeShip(new Vector3(-20f, 0f, 0f));
            FakeShip second = new FakeShip(new Vector3(20f, 0f, 0f));
            GameObject targetView = new GameObject("Target");
            targetView.transform.position = new Vector3(100f, 0f, 50f);
            FakeEntity target = new FakeEntity(
                1,
                PlayerType.Player,
                new FakeHealthModel(targetView.transform));
            EnemyStrategicContext context = new EnemyStrategicContext(
                default,
                new IShipEntity[] { first, second },
                default,
                target,
                null,
                null);

            try
            {
                new EnemyTaskForceExecutor().Execute(
                    new EnemyStrategicDecision(
                        EnemyStrategicState.HuntFleet,
                        2,
                        "test"),
                    context);

                Assert.That(first.AssignedAttackTarget, Is.SameAs(target));
                Assert.That(second.AssignedAttackTarget, Is.SameAs(target));
                Assert.That(
                    first.AssignedAttackOffset,
                    Is.Not.EqualTo(second.AssignedAttackOffset));
                Assert.That(
                    Vector3.Distance(
                        first.AssignedAttackOffset,
                        second.AssignedAttackOffset),
                    Is.GreaterThanOrEqualTo(
                        first.NavigationRadius + second.NavigationRadius));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
            }
        }

        private sealed class FakeGameModel : IGameModelObserver
        {
            public EmpireAtWar.Entities.Planet.PlanetType PlanetType => default;
            public FactionType PlayerFactionType => default;
            public FactionType EnemyFactionType => default;
            public BattleVictoryCondition VictoryCondition => BattleVictoryCondition.DestroyEnemyFleet;
            public EnemyAiDifficulty EnemyDifficulty => EnemyAiDifficulty.UltraHard;
            public float StartingMoney => 1000f;
        }

        private sealed class FakeEntityLocator : IEntityLocator
        {
            public event Action<IEntity> EntityAdded;
            public event Action<IEntity> EntityRemoved;
            public string Id => nameof(FakeEntityLocator);
            public IReadOnlyCollection<IEntity> Entities { get; } = Array.Empty<IEntity>();

            public void AddEntity(IEntity entity)
            {
                EntityAdded?.Invoke(entity);
            }

            public void RemoveEntity(IEntity entity)
            {
                EntityRemoved?.Invoke(entity);
            }

            public IEntity GetEntity(long entityId)
            {
                throw new InvalidOperationException();
            }

            public bool TryGetEntity(RaycastHit raycastHit, out IEntity entity)
            {
                entity = null;
                return false;
            }

            public bool TryGetEntity(Collider collider, out IEntity entity)
            {
                entity = null;
                return false;
            }
        }

        private sealed class FakeShip : IShipEntity
        {
            public FakeShip(Vector3 worldPosition)
            {
                WorldPosition = worldPosition;
            }

            public IShipModelObserver ModelObserver => null;
            public PlayerType PlayerType => PlayerType.Opponent;
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 5f;
            public Vector3 AssignedMoveTarget { get; private set; }
            public IEntity AssignedAttackTarget { get; private set; }
            public Vector3 AssignedAttackOffset { get; private set; }

            public void AssignAttackTarget(
                IEntity target,
                Vector3 formationOffset)
            {
                AssignedAttackTarget = target;
                AssignedAttackOffset = formationOffset;
            }

            public void AssignMoveTarget(Vector3 target)
            {
                AssignedMoveTarget = target;
            }

            public void HoldPosition()
            {
            }
        }

        private sealed class FakeShipService : IShipService
        {
            public FakeShipService(params IShipEntity[] ships)
            {
                Ships = ships;
            }

            public event Action<IShipEntity> ShipAdded;
            public event Action<IShipEntity> ShipRemoved;
            public string Id => nameof(FakeShipService);
            public IReadOnlyList<IShipEntity> Ships { get; }

            public void Add(IShipEntity entity)
            {
                ShipAdded?.Invoke(entity);
            }

            public void Remove(IShipEntity entity)
            {
                ShipRemoved?.Invoke(entity);
            }
        }

        private sealed class FakeReinforcementZonesSystem : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged;

            public bool IsPositionInAnyZone(Vector3 position)
            {
                return false;
            }

            public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position)
            {
                return false;
            }

            public bool IsShipSpawnPositionClear(
                ShipType shipType,
                Vector3 position)
            {
                return true;
            }

            public bool TryGetRandomSpawnPosition(
                PlayerType playerType,
                ShipType shipType,
                out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
            {
                position = new Vector3(55f, 0f, -55f);
                return true;
            }
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(
                long id,
                PlayerType playerType,
                IHealthModelObserver healthModel)
            {
                Id = id;
                PlayerType = playerType;
                HealthModel = healthModel;
            }

            public long Id { get; }
            public EmpireAtWar.Mvc.IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType { get; }

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealthModel : IHealthModelObserver
        {
            public FakeHealthModel(Transform transform)
            {
                Transform = transform;
            }

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
            public Transform Transform { get; }
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
