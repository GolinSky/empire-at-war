using DG.Tweening;
using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class SkirmishCameraView : View<ISkirmishCameraModelObserver, ICameraCommand>
    {
        [SerializeField] private Camera mainCamera;

        [SerializeField] private Canvas cameraCanvas;
        [SerializeField] private Button switcherButton;
        [SerializeField] private Button zoomIn;
        [SerializeField] private Button zoomOut;
        [SerializeField] private Button maxZoomIn;
        [SerializeField] private Button minZoomOut;
        [SerializeField] private Ease moveEase;

        private Sequence moveSequence;
        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;
        
        
        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
            switcherButton.gameObject.SetActive(Application.isEditor);// debug
            Model.OnTranslateDirectionChanged += Translate;
            Model.OnPositionChanged += SetPosition;
            Model.OnFovChanged += UpdateFieldOfView;
            zoomIn.onClick.AddListener(Command.ZoomIn);
            zoomOut.onClick.AddListener(Command.ZoomOut);
            maxZoomIn.onClick.AddListener(Command.MaxZoomIn);
            minZoomOut.onClick.AddListener(Command.MaxZoomOut);
            switcherButton.onClick.AddListener(UpdateCanvasState);
        }

        protected override void OnDispose()
        {
            Model.OnTranslateDirectionChanged -= Translate;
            Model.OnPositionChanged -= SetPosition;
            Model.OnFovChanged -= UpdateFieldOfView;
            zoomIn.onClick.RemoveListener(Command.ZoomIn);
            zoomOut.onClick.RemoveListener(Command.ZoomOut);
            maxZoomIn.onClick.RemoveListener(Command.MaxZoomIn);
            minZoomOut.onClick.RemoveListener(Command.MaxZoomOut);
            switcherButton.onClick.RemoveListener(UpdateCanvasState);
        }
        
        private void UpdateFieldOfView(float fieldOfView)
        {
            mainCamera.fieldOfView = fieldOfView;
        }
        
        private void UpdateCanvasState()
        {
            cameraCanvas.enabled = !cameraCanvas.enabled;
        }
        
        private void SetPosition(Vector3 position)
        {
            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(cameraTransform
                .DOMove(position, Model.MoveSpeed)
                .SetEase(moveEase));
        }

        private void Translate(Vector3 direction)
        {
            cameraTransform.Translate(direction, Space.World);
        }
    }
}