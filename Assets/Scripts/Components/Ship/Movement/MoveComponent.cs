using EmpireAtWar.Models.Movement;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class MoveComponent : BaseComponent<MoveModel>, IMovable
    {
        private readonly ICameraService cameraService;
        public bool CanMove => true;

        public MoveComponent(IModel model, ICameraService cameraService) : base(model)
        {
            this.cameraService = cameraService;
            Vector3 tempPosition = Vector3.zero;
            tempPosition.y = Model.Height;
            Model.HyperSpacePosition = tempPosition;
            Model.Position = tempPosition;
        }
        
        public void MoveToPosition(Vector2 screenPosition)
        {
            Model.Position = GetWorldCoordinate(screenPosition);
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = cameraService.GetWorldPoint(screenPosition);
            point.y = Model.Height;
            return point;
        }
    }
}