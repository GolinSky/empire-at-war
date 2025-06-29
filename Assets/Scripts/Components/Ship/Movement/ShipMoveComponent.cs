using EmpireAtWar.Commands.Move;
using EmpireAtWar.Services.Camera;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : BaseComponent<ShipMoveModel>, IShipMoveComponent, IMoveCommand, IInitializable
    {
        private readonly ICameraService _cameraService;
        private Vector3 _startPosition;
        public bool CanMove => Model.CanMove;

        
        public ShipMoveComponent(IModel model, ICameraService cameraService, Vector3 startPosition) : base(model)
        {
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            // Model.TargetPosition = startPosition;
        }
        
        public void Initialize()
        {
            Model.HyperSpacePosition = _startPosition;
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
    }
}