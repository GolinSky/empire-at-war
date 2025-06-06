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
        private Vector3 _translateDirection;
        private Vector3 _cameraPosition;
        private bool _moved;
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
            _inputService.OnInput += HandleInput;
            _inputService.OnSwipe += OnSwipe;
            _inputService.OnZoom += ZoomCamera;
        }
        

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
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
        
        private void ZoomCamera(InputType inputType, Touch firstTouch, Touch secondTouch)
        {
            if (inputType != InputType.CameraInput) return;

            Vector2 touchZeroPrevPos = firstTouch.position - firstTouch.deltaPosition;
            Vector2 touchOnePrevPos = secondTouch.position - secondTouch.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (firstTouch.position - secondTouch.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            Vector3 newPos = _cameraService.CameraPosition - _cameraService.CameraForward * deltaMagnitudeDiff * Model.ZoomSpeed * Time.unscaledDeltaTime;
            if(Model.ZoomRange.IsInRange(newPos.y))
            {
                newPos.y = Model.ZoomRange.Clamp(newPos.y);
                Model.CameraPosition = ClampPosition(newPos);
            }
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


        private void HandleInput(InputType inputType, TouchPhase phase, Vector2 screenPosition)
        {
            if (inputType != InputType.CameraInput) return;

            switch (phase)
            {
                case TouchPhase.Moved:
                {
                    Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
                    Vector3 move = new Vector3(-touchDeltaPosition.x, 0, -touchDeltaPosition.y) * Model.PanSpeed * Time.unscaledDeltaTime;
                    Model.CameraPositionUsingTween = ClampPosition(move+Position);
                    break;
                }
            }
        }

        public void MoveTo(Vector3 worldPoint)
        {
            double b = Position.y * Mathf.Sin(Transform.rotation.eulerAngles.x); 
            worldPoint.z += (float)b;
            worldPoint.y = Position.y;
            Model.CameraPositionUsingTween = ClampPosition(worldPoint);
        }
        

        private Vector3 ClampPosition(Vector3 position)
        {
            float height = Position.y;
            float heightPercentage = Mathf.InverseLerp(Model.ZoomRange.Min, Model.ZoomRange.Max, height);
            return Model.ClampPosition(heightPercentage, position);
        }
    }
}