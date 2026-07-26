using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
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
            EnemyUnitCommander commander = new EnemyUnitCommander(
                shipService,
                new FakeReinforcementZonesSystem());

            commander.Initialize();

            Assert.That(first.AssignedMoveTarget, Is.Not.EqualTo(second.AssignedMoveTarget));
            Assert.That(first.AssignedMoveTarget.x, Is.EqualTo(49f));
            Assert.That(second.AssignedMoveTarget.x, Is.EqualTo(61f));
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

            public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
            {
                position = new Vector3(55f, 0f, -55f);
                return true;
            }
        }
    }
}
