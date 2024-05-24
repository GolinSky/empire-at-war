using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Services.Camera;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class ShipMoveComponent : BaseComponent<ShipMoveModel>, IShipMoveComponent, IMoveCommand, IInitializable
    {
        private readonly ICameraService cameraService;
        private Transform transform;
        private Vector3 startPosition;
        public bool CanMove => Model.CanMove;

        public ShipMoveComponent(IModel model, ICameraService cameraService, Vector3 startPosition) : base(model)
        {
            this.cameraService = cameraService;
            startPosition.y = Model.Height;
            this.startPosition = startPosition;
            // Model.TargetPosition = startPosition;
        }
        
        public void Initialize()
        {
            Model.HyperSpacePosition = startPosition;
        }
        
        public void MoveToPosition(Vector2 screenPosition)
        {
            GetWorldCoordinate(screenPosition); 
            Model.TargetPosition = GetWorldCoordinate(screenPosition);
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = cameraService.GetWorldPoint(screenPosition, Model.CurrentPosition);
            point.y = Model.Height;

            return point;
        }

        public float MoveAround()
        {
            Vector3 backPosition = Model.CurrentPosition - Model.ViewTransform.Value.forward * Random.Range(30, 50f) + transform.right * Random.Range(-30, 30);
            Model.TargetPosition = backPosition;
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
            Model.TargetPosition = targetPosition;
        }

        public void MoveToPositionOnScreen(Vector2 targetPosition)
        {
            MoveToPosition(targetPosition);
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Model.LookAtTarget = targetPosition;
        }

     
    }
}