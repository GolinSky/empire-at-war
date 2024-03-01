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


        [SerializeField] private Ease moveEase;

        private Sequence moveSequence;
        private Transform cameraTransform;
        private Vector3 worldStartPoint;
        private Vector3 cameraPosition;
        
        
        protected override void OnInitialize()
        {
            cameraTransform = mainCamera.transform;
            Model.OnTranslateDirectionChanged += Translate;
            Model.OnPositionChanged += SetPosition;
            Model.OnFovChanged += UpdateFieldOfView;
        }

        protected override void OnDispose()
        {
            Model.OnTranslateDirectionChanged -= Translate;
            Model.OnPositionChanged -= SetPosition;
            Model.OnFovChanged -= UpdateFieldOfView;
        }
        
        private void UpdateFieldOfView(float fieldOfView)
        {
            mainCamera.fieldOfView = fieldOfView;
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