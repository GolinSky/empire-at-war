using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.SkirmishCamera
{
    public class CoreCameraController : Controller<CoreCameraModel>, IInitializable, ILateDisposable, ICameraCommand
    {
        private readonly ICameraService cameraService;
        private readonly IInputService inputService;
        private Vector3 translateDirection;
        private Vector3 cameraPosition;
        private bool moved;
        [Inject(Id = EntityBindType.ViewTransform)]
        private Transform Transform { get; }

        private Vector3 Position => Transform.position;

        public CoreCameraController(
            CoreCameraModel model,
            ICameraService cameraService,
            IInputService inputService) : base(model)
        {
            this.cameraService = cameraService;
            this.inputService = inputService;
            cameraService.AddCommand(this);
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
            inputService.OnDoubleInput += ZoomCamera;
        }
        
        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
            inputService.OnDoubleInput -= ZoomCamera;
        }
        
        private void ZoomCamera(InputType inputType, Touch firstTouch, Touch secondTouch)
        {
            if (inputType != InputType.CameraInput) return;

            Vector2 touchZeroPrevPos = firstTouch.position - firstTouch.deltaPosition;
            Vector2 touchOnePrevPos = secondTouch.position - secondTouch.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (firstTouch.position - secondTouch.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            Vector3 newPos = cameraService.CameraPosition - cameraService.CameraForward * deltaMagnitudeDiff * Model.ZoomSpeed * Time.unscaledDeltaTime;
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