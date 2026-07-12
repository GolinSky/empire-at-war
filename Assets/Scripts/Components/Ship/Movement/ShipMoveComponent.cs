using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : BaseComponent<ShipMoveModel>, IShipMoveComponent, IInitializable
    {
        private readonly ICameraService _cameraService;
        private readonly Vector3 _startPosition;
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly PlayerType _playerType;
        public bool CanMove => Model.CanMove;
        public Vector3 CurrentPosition => Model.CurrentPosition;
        public Transform ViewTransform => Model.ViewTransform.Value;
        public bool IsMoving => Model.IsMoving;
        public float HyperSpaceDuration => Model.HyperSpaceDuration;

        public event System.Action<Vector3> TargetPositionChanged
        {
            add => Model.TargetPosition.OnChanged += value;
            remove => Model.TargetPosition.OnChanged -= value;
        }

        public event System.Action<Vector3> LookAtTargetChanged
        {
            add => Model.LookAtTarget.OnChanged += value;
            remove => Model.LookAtTarget.OnChanged -= value;
        }

        public event System.Action Stopped
        {
            add => Model.OnStop += value;
            remove => Model.OnStop -= value;
        }


        public ShipMoveComponent(
            IModel model,
            ICameraService cameraService,
            Vector3 startPosition,
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType) : base(model)
        {
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            // Model.TargetPosition = startPosition;
        }

        public void Initialize()
        {
            Model.HyperSpacePosition = _startPosition;

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(Model.ViewTransform.Value, 80f);
            }
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            Vector3 newPosition = GetWorldCoordinate(screenPosition);
            SetTargetPosition(newPosition);
        }

        private void SetTargetPosition(Vector3 newPosition)
        {
            if (!newPosition.IsEqual(Model.TargetPosition.Value))
            {
                Model.TargetPosition.Value = newPosition;
            }
        }
        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, Model.CurrentPosition);
            point.y = Model.Height;

            return point;
        }

        public float MoveAround()
        {
            Vector3 backPosition = Model.CurrentPosition - Model.ViewTransform.Value.forward * Random.Range(30, 50f) + Model.ViewTransform.Value.right * Random.Range(-30, 30);
            SetTargetPosition(backPosition);

            return Vector3.Distance(backPosition, Model.CurrentPosition) / Model.Speed;
        }

        public Vector3 CalculateLookDirection(Vector3 targetPosition)
        {
            targetPosition.y = Model.Height;
            return targetPosition - Model.CurrentPosition;
        }

        public void MoveToPosition(Vector3 targetPosition)
        {
            targetPosition.y = Model.Height;
            SetTargetPosition(targetPosition);
        }

        public void MoveToPositionOnScreen(Vector2 targetPosition)
        {
            MoveToPosition(targetPosition);
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Model.LookAtTarget.Value = targetPosition;
        }

        public float GetRange(Vector3 targetPosition)
        {
            return Vector3.Distance(Model.CurrentPosition, targetPosition);
        }

        public void Stop()
        {
            Model.Stop();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            Model.ApplyMoveCoefficient(coefficient);
        }
    }
}
