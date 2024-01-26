using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.SkirmishCamera
{
    public class SkirmishCameraController : Controller<SkirmishCameraModel>, IInitializable, ILateDisposable
    {
        private readonly ICameraService cameraService;
        private readonly IInputService inputService;
        private Vector3 worldStartPoint;
        private Vector3 translateDirection;
        private Vector3 cameraPosition;

        public SkirmishCameraController(
            SkirmishCameraModel model,
            ICameraService cameraService,
            IInputService inputService) : base(model)
        {
            this.cameraService = cameraService;
            this.inputService = inputService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
        }

        private void HandleInput(InputType inputType, TouchPhase phase, Vector2 screenPosition)
        {
            if (inputType != InputType.CameraInput) return;

            switch (phase)
            {
                case TouchPhase.Began:
                {
                    worldStartPoint = cameraService.GetWorldPoint(screenPosition);
                    break;
                }
                case TouchPhase.Moved:
                {
                    Vector3 worldDelta = cameraService.GetWorldPoint(screenPosition) - worldStartPoint;

                    translateDirection.x = -worldDelta.x;
                    translateDirection.y = 0;
                    translateDirection.z = -worldDelta.z;

                    Model.TranslateDirection = translateDirection;

                    cameraPosition = cameraService.CameraPosition;

                    cameraPosition.x = Mathf.Clamp(cameraPosition.x, -Model.MapSize.x / 2.0f, Model.MapSize.x / 2.0f);
                    cameraPosition.y = Mathf.Clamp(cameraPosition.y, -Model.MapSize.y / 2.0f, Model.MapSize.y / 2.0f);
                    cameraPosition.z = Mathf.Clamp(cameraPosition.z, -Model.MapSize.z / 2.0f, Model.MapSize.z / 2.0f);

                    Model.CameraPosition = cameraPosition;
                    break;
                }
            }
        }
    }
}