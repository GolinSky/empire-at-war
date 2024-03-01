using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using LightWeightFramework.Controller;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.SkirmishCamera
{
    public class SkirmishCameraController : Controller<SkirmishCameraModel>, IInitializable, ILateDisposable, ICameraCommand, ITickable
    {
        private const float DelayAfterZoom = 0.1f;
        
        private readonly ICameraService cameraService;
        private readonly IInputService inputService;
        private readonly ITimer inputDelay;
        private Vector3 worldStartPoint;
        private Vector3 translateDirection;
        private Vector3 cameraPosition;
        private bool moved;

        public SkirmishCameraController(
            SkirmishCameraModel model,
            ICameraService cameraService,
            IInputService inputService) : base(model)
        {
            this.cameraService = cameraService;
            this.inputService = inputService;
            inputDelay = TimerFactory.ConstructTimer(DelayAfterZoom);
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

            Vector2 previousFirstPosition = firstTouch.position - firstTouch.deltaPosition;
            Vector2 previousSecondPosition = secondTouch.position - secondTouch.deltaPosition;

            float oldTouchDistance = Vector2.Distance (previousFirstPosition, previousSecondPosition);
            float currentTouchDistance = Vector2.Distance (firstTouch.position, secondTouch.position);

            float deltaDistance = oldTouchDistance - currentTouchDistance;
            float fieldOfView = cameraService.FieldOfView;
            
            fieldOfView += deltaDistance * Model.ZoomSpeed * Time.deltaTime;
            Model.FieldOfView = Model.ZoomRange.Clamp(fieldOfView);

            inputDelay.StartTimer();
        }

        private void HandleInput(InputType inputType, TouchPhase phase, Vector2 screenPosition)
        {
            if (!inputDelay.IsComplete)
            {
                worldStartPoint = cameraService.GetWorldPoint(screenPosition);
            }
            
            if (inputType != InputType.CameraInput) return;

            switch (phase)
            {
                case TouchPhase.Began:
                {
                    worldStartPoint = cameraService.GetWorldPoint(screenPosition);
                    worldStartPoint.y = 0;
                    moved = true;
                    break;
                }
                case TouchPhase.Moved:
                {
                    if(!moved) return;
                    
                    Vector3 worldDelta = cameraService.GetWorldPoint(screenPosition) - worldStartPoint;
                    translateDirection.x = -worldDelta.x;
                    translateDirection.y = 0;
                    translateDirection.z = -worldDelta.z;

                    Model.TranslateDirection = translateDirection;

                    cameraPosition = cameraService.CameraPosition;

                    cameraPosition.x = Mathf.Clamp(cameraPosition.x, -Model.MapSize.x / 2.0f, Model.MapSize.x / 2.0f); // todo: use map entity constrains - delete divide by 2
                    cameraPosition.y = Mathf.Clamp(cameraPosition.y, 20, Model.MapSize.y );
                    cameraPosition.z = Mathf.Clamp(cameraPosition.z, -Model.MapSize.z / 2.0f, Model.MapSize.z / 2.0f);

                    Model.CameraPosition = cameraPosition;
                    break;
                }
            }

            if (phase == TouchPhase.Canceled || phase == TouchPhase.Ended)
            {
                moved = false;
            }
        }

        public void Tick()
        {
#if UNITY_EDITOR
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                float fieldOfView = cameraService.FieldOfView;

                fieldOfView -= Input.mouseScrollDelta.y * Model.ZoomSpeed * 40f* Time.deltaTime;
                Model.FieldOfView = Model.ZoomRange.Clamp(fieldOfView);
            }   
#endif
        }
    }
}