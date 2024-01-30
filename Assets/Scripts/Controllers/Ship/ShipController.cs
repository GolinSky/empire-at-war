using EmpireAtWar.Models.Ship;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Controllers.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
    }
    public class ShipController:Controller<ShipModel>, IInitializable, ILateDisposable, ISelectable, IShipEntity
    {
        private readonly IShipService shipService;
        private readonly ICameraService cameraService;

        public IShipModelObserver ModelObserver => Model;

        public bool CanMove => true;
        
        public ShipController(ShipModel model, IShipService shipService, ICameraService cameraService) : base(model)
        {
            this.shipService = shipService;
            this.cameraService = cameraService;

            Vector3 tempPosition = Vector3.zero;
            tempPosition.y = Model.Height;
            Model.HyperSpacePosition = tempPosition;
        }

        public void Initialize()
        {
            shipService.Add(this);
        }
        
        public void LateDispose()
        {
            shipService.Remove(this);
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            MoveToPosition(GetWorldCoordinate(screenPosition));
        }

        private void MoveToPosition(Vector3 worldPosition)
        {
            Model.Position = worldPosition;
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = cameraService.GetWorldPoint(screenPosition);
            point.y = Model.Height;
            return point;
        }
    }
}