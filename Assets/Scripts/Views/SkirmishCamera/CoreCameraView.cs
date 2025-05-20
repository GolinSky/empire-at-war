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

        private Sequence _moveSequence;
        private Transform _cameraTransform;
        private Vector3 _worldStartPoint;
        private Vector3 _cameraPosition;
        
        
        protected override void OnInitialize()
        {
            _cameraTransform = mainCamera.transform;
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
            _moveSequence.KillIfExist();
            DOTween.Kill(transform);
            _moveSequence = DOTween.Sequence();
            if (useTweens)
            {
                _moveSequence.Append(_cameraTransform.DOMove(position, Model.TweenSpeed).SetEase(moveEase));
            }
            else
            {
                _cameraTransform.position = position;
            }
        }
    }
}