using DG.Tweening;
using EmpireAtWar.Commands.Camera;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

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

        private Vector2 ToXZ(Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }
        private void SetPosition(Vector3 position)
        {
            Vector2 clampedPosition = Model.MoveRange.Clamp(ToXZ(position));
            position.x = clampedPosition.x;
            position.z = clampedPosition.y;
            cameraTransform.position = position;
        }

        private void Translate(Vector3 direction)
        {
            SetPosition(transform.position + direction);
        }
    }
}