using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class SkirmishCameraView : View<ISkirmishCameraModelObserver>, ITickable
    {
        [SerializeField] private Camera mainCamera;

        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;

        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
        }
        protected override void OnDispose() {}
       
        public void Tick()
        {
            if (Input.touchCount == 1)
            {
                Touch currentTouch = Input.GetTouch(0);

                if (currentTouch.phase == TouchPhase.Began)
                {
                    worldStartPoint = GetWorldPoint(currentTouch.position);
                }

                if (currentTouch.phase == TouchPhase.Moved)
                {
                    Vector3 worldDelta = GetWorldPoint(currentTouch.position) - worldStartPoint;
                    cameraTransform.Translate(
                        -worldDelta.x,
                        0,
                        -worldDelta.z,
                        Space.World
                    );
                    cameraPosition = cameraTransform.position;

                    cameraPosition.x = Mathf.Clamp(cameraPosition.x, -Model.MapSize.x / 2.0f, Model.MapSize.x / 2.0f);
                    cameraPosition.y = Mathf.Clamp(cameraPosition.y, -Model.MapSize.y / 2.0f, Model.MapSize.y / 2.0f);
                    cameraPosition.z = Mathf.Clamp(cameraPosition.z, -Model.MapSize.z / 2.0f, Model.MapSize.z / 2.0f);

                    cameraTransform.position = cameraPosition;
                }
            }
        }
        
        private Vector3 GetWorldPoint(Vector2 screenPoint)
        {
            Physics.Raycast(mainCamera.ScreenPointToRay(screenPoint), out RaycastHit hit);
            return hit.point;
        }
    }
}