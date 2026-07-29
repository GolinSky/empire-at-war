using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStrategicContext
    {
        public EnemyStrategicContext(
            EnemyStrategicSnapshot snapshot,
            IReadOnlyList<IShipEntity> ships,
            Vector3 captureTarget,
            GameEntity enemyFleetTarget,
            GameEntity enemyBaseTarget,
            GameEntity ownBase)
        {
            Snapshot = snapshot;
            Ships = ships ?? throw new ArgumentNullException(nameof(ships));
            CaptureTarget = captureTarget;
            EnemyFleetTarget = enemyFleetTarget;
            EnemyBaseTarget = enemyBaseTarget;
            OwnBase = ownBase;
        }

        public EnemyStrategicSnapshot Snapshot { get; }
        public IReadOnlyList<IShipEntity> Ships { get; }
        public Vector3 CaptureTarget { get; }
        public GameEntity EnemyFleetTarget { get; }
        public GameEntity EnemyBaseTarget { get; }
        public GameEntity OwnBase { get; }
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;

        public EnemyStrategicContextBuilder(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel)
        {
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _reinforcementZonesSystem = reinforcementZonesSystem ??
                throw new ArgumentNullException(nameof(reinforcementZonesSystem));
            _entityLocator = entityLocator ?? throw new ArgumentNullException(nameof(entityLocator));
            _gameModel = gameModel ?? throw new ArgumentNullException(nameof(gameModel));
        }

        public EnemyStrategicContext Build()
        {
            List<IShipEntity> enemyShips = GetShips(PlayerType.Opponent);
            List<IShipEntity> playerShips = GetShips(PlayerType.Player);
            FormationPoint fleetCenter = CalculateFleetCenter(enemyShips);
            Vector3 origin = new Vector3(fleetCenter.X, 0f, fleetCenter.Z);
            bool hasCaptureTarget = _reinforcementZonesSystem.TryGetCaptureTarget(
                PlayerType.Opponent,
                origin,
                out Vector3 captureTarget);
            GameEntity enemyBaseTarget = FindClosestEntity<ISpaceStationModelObserver>(
                PlayerType.Player,
                origin);
            GameEntity ownBase = FindClosestEntity<ISpaceStationModelObserver>(
                PlayerType.Opponent,
                origin);
            GameEntity enemyFleetTarget = FindClosestEntity<IShipModelObserver>(
                PlayerType.Player,
                origin);

            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                _gameModel.VictoryCondition,
                _gameModel.EnemyDifficulty,
                enemyShips.Count,
                playerShips.Count,
                hasCaptureTarget,
                enemyBaseTarget != null,
                ownBase != null);
            return new EnemyStrategicContext(
                snapshot,
                enemyShips,
                captureTarget,
                enemyFleetTarget,
                enemyBaseTarget,
                ownBase);
        }

        private List<IShipEntity> GetShips(PlayerType playerType)
        {
            List<IShipEntity> ships = new List<IShipEntity>();
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == playerType)
                {
                    ships.Add(ship);
                }
            }

            return ships;
        }

        private static FormationPoint CalculateFleetCenter(IReadOnlyList<IShipEntity> ships)
        {
            if (ships.Count == 0)
            {
                return new FormationPoint(0f, 0f);
            }

            List<FormationPoint> positions = new List<FormationPoint>(ships.Count);
            foreach (IShipEntity ship in ships)
            {
                positions.Add(new FormationPoint(ship.WorldPosition.x, ship.WorldPosition.z));
            }

            return FormationModel.CalculateCenter(positions);
        }

        private GameEntity FindClosestEntity<TModel>(PlayerType playerType, Vector3 origin)
            where TModel : IModelObserver
        {
            GameEntity closest = null;
            float closestDistance = float.MaxValue;
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (entity.PlayerType != playerType ||
                    entity.Model is not TModel ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits)
                {
                    continue;
                }

                float distance = (entity.HealthModel.Transform.position - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = entity;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
