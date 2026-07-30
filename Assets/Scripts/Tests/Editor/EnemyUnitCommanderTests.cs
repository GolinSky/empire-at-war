using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;

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
            public Vector3 AssignedMoveTarget { get; private set; }

            public void AssignAttackTarget(IEntity target)
            {
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

            public bool TryGetRandomSpawnPosition(PlayerType playerType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position)
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
    }
}
