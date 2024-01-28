using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class SkirmishCameraView : View<ISkirmishCameraModelObserver, ICameraCommand>
    {
        [SerializeField] private Camera mainCamera;

        [SerializeField] private Button zoomIn;
        [SerializeField] private Button zoomOut;
        
        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;

        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
            Model.OnTranslateDirectionChanged += Translate;
            Model.OnPositionChanged += SetPosition;
            zoomIn.onClick.AddListener(Command.ZoomIn);
            zoomOut.onClick.AddListener(Command.ZoomOut);
        }

        protected override void OnDispose()
        {
            Model.OnTranslateDirectionChanged -= Translate;
            Model.OnPositionChanged -= SetPosition;
            zoomIn.onClick.RemoveListener(Command.ZoomIn);
            zoomOut.onClick.RemoveListener(Command.ZoomOut);
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