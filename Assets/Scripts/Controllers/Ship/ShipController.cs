using EmpireAtWar.Models.Ship;
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
        private Plane plane;
        private float customHeightDelta;//temp solution

        public IShipModelObserver ModelObserver => Model;

        public bool CanMove => true;
        
        public ShipController(ShipModel model, IInputService inputService, IShipService shipService) : base(model)
        {
            this.shipService = shipService;
            var m_DistanceFromCamera = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - 50, Camera.main.transform.position.z );

            plane = new Plane(Vector3.up, m_DistanceFromCamera);
            customHeightDelta = Random.Range(-10, 10);
            Model.HyperSpacePosition = GetWorldCoordinate(inputService.MouseCoordinates);
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
            worldPosition.y += customHeightDelta;
            Model.Position = worldPosition;
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 screenPosition3d = screenPosition;
            screenPosition3d.z = Camera.main.transform.position.y;
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPosition3d);
            worldPoint.y = ModelObserver.Position.y;
            return worldPoint;
        }
    }
}