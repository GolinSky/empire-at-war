using DG.Tweening;
using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.Views.SkirmishCamera
{
    public class CoreCameraView : View<ICoreCameraModelObserver, ICameraCommand>
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Ease moveEase;//out expo

        private Sequence moveSequence;
        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;
        
        
        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
            Model.OnPositionChanged += SetPosition;
            Model.OnFovChanged += UpdateFieldOfView;
        }

        protected override void OnDispose()
        {
            Model.OnPositionChanged -= SetPosition;
            Model.OnFovChanged -= UpdateFieldOfView;
        }
        
        private void UpdateFieldOfView(float fieldOfView)
        {
            mainCamera.fieldOfView = fieldOfView;
        }
        
        private void SetPosition(Vector3 position, bool useTweens)
        {
            moveSequence.KillIfExist();
            DOTween.Kill(transform);
          
            if (useTweens)
            {
                moveSequence.Append(cameraTransform.DOMove(position, Model.TweenSpeed).SetEase(moveEase));
            }
            else
            {
                cameraTransform.position = position;
            }
        }
    }
}