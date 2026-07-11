using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SkirmishCamera
{
    public class CoreCameraController : Controller<CoreCameraModel>, IInitializable, ILateDisposable, ICameraCommand
    {
        private readonly ICameraService _cameraService;
        private readonly IInputService _inputService;
        [Inject(Id = EntityBindType.ViewTransform)]
        private Transform Transform { get; }

        private Vector3 Position => Transform.position;

        public CoreCameraController(
            CoreCameraModel model,
            ICameraService cameraService,
            IInputService inputService) : base(model)
        {
            _cameraService = cameraService;
            _inputService = inputService;
            cameraService.AddCommand(this);
        }

        public void Initialize()
        {
            _inputService.OnSwipe += OnSwipe;
            _inputService.OnZoom += ZoomCamera;
        }
        

        public void LateDispose()
        {
            _inputService.OnSwipe -= OnSwipe;
            _inputService.OnZoom -= ZoomCamera;
        }
        
        private void OnSwipe(Vector2 direction)
        {
            Vector3 worldDirection = Vector3.zero;
            worldDirection.x = direction.x;
            worldDirection.z = direction.y;
            Vector3 move = -worldDirection * Model.PanSpeed * Time.unscaledDeltaTime;
            Model.CameraPositionUsingTween = ClampPosition(move+Position);
        }
        
        private void ZoomCamera(float scrollDelta)
        {
            scrollDelta = Mathf.Clamp(scrollDelta, -10, 10);
            Vector3 newPos = _cameraService.CameraPosition - _cameraService.CameraForward * scrollDelta * Model.ZoomSpeed * Time.unscaledDeltaTime;
            if(Model.ZoomRange.IsInRange(newPos.y))
            {
                newPos.y = Model.ZoomRange.Clamp(newPos.y);
                Model.CameraPosition = ClampPosition(newPos);
            }
        }

        public void MoveTo(Vector3 worldPoint)
        {
            Vector3 forward = Transform.forward;
            Vector3 targetCameraPosition = worldPoint;

            if (Mathf.Abs(forward.y) > Mathf.Epsilon)
            {
                float distanceToTargetPlane = (worldPoint.y - Position.y) / forward.y;
                targetCameraPosition = worldPoint - forward * distanceToTargetPlane;
            }

            targetCameraPosition.y = Position.y;
            Model.CameraPositionUsingTween = ClampPosition(targetCameraPosition);
        }
        

        private Vector3 ClampPosition(Vector3 position)
        {
            float height = Position.y;
            float heightPercentage = Mathf.InverseLerp(Model.ZoomRange.Min, Model.ZoomRange.Max, height);
            return Model.ClampPosition(heightPercentage, position);
        }
    }
}
