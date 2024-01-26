using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class SkirmishCameraView : View<ISkirmishCameraModelObserver>
    {
        [SerializeField] private Camera mainCamera;

        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;

        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
            Model.OnTranslateDirectionChanged += Translate;
            Model.OnPositionChanged += SetPosition;
        }

        protected override void OnDispose()
        {
            Model.OnTranslateDirectionChanged -= Translate;
            Model.OnPositionChanged -= SetPosition;
        }
        
        private void SetPosition(Vector3 position)
        {
            cameraTransform.position = position;
        }

        private void Translate(Vector3 direction)
        {
            cameraTransform.Translate(direction, Space.World);
        }
    }
}