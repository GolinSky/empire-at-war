using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class FleeState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IMapModelObserver _mapModel;
        private readonly FactionType _factionType;

        public FleeState(
            IShipMoveComponent shipMoveComponent,
            IMapModelObserver mapModel,
            PlayerType playerType,
            IGameModelObserver gameModel)
        {
            _shipMoveComponent = shipMoveComponent ??
                throw new ArgumentNullException(nameof(shipMoveComponent));
            _mapModel = mapModel ?? throw new ArgumentNullException(nameof(mapModel));
            if (gameModel == null)
            {
                throw new ArgumentNullException(nameof(gameModel));
            }

            _factionType = playerType switch
            {
                PlayerType.Player => gameModel.PlayerFactionType,
                PlayerType.Opponent => gameModel.EnemyFactionType,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(playerType),
                    playerType,
                    null)
            };
        }

        public void Enter()
        {
            Vector3 safePosition = _mapModel.GetStationPosition(_factionType);
            _shipMoveComponent.MoveToPosition(safePosition);
        }

        public void Update()
        {
            // Just keep moving to the base if we are not moving
            if (!_shipMoveComponent.IsMoving)
            {
                Vector3 safePosition = _mapModel.GetStationPosition(_factionType);
                _shipMoveComponent.MoveToPosition(safePosition);
            }
        }

        public void Exit()
        {
            _shipMoveComponent.Stop();
        }
    }
}
