using EmpireAtWar.Commands.Camera;
using UnityEngine;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position);
        RaycastHit ScreenPointToRay(Vector2 screenPoint);
        Vector3 CameraPosition { get; }
        Transform CameraTransform { get; }
        Vector3 CameraForward { get; }
        float FieldOfView { get; }
        Vector3 WorldToViewportPoint(Vector3 currentPosition);
        Vector2 WorldToScreenPoint(Vector3 position);
        void MoveTo(Vector3 worldPoint);
        void AddCommand(ICameraCommand cameraCommand);
    }

    public class CameraService : Service, ICameraService
    {
        private  ICameraCommand _cameraCommand;
        private readonly UnityEngine.Camera _camera;
        private Plane _plane = new Plane();

        public Vector3 CameraPosition => _camera.transform.position;
        public Transform CameraTransform => _camera.transform;
        public Vector3 CameraForward => _camera.transform.forward;
        public float FieldOfView => _camera.fieldOfView;

        public CameraService(UnityEngine.Camera camera)
        {
            _camera = camera;
        }
        
        public Vector3 WorldToViewportPoint(Vector3 currentPosition)
        {
            return _camera.WorldToViewportPoint(currentPosition);
        }

        public Vector2 WorldToScreenPoint(Vector3 position)
        {
            return _camera.WorldToScreenPoint(position);
        }

        public void MoveTo(Vector3 worldPoint)
        {
            _cameraCommand.MoveTo(worldPoint);
        }

        public void AddCommand(ICameraCommand cameraCommand)
        {
            _cameraCommand = cameraCommand;
        }

        public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position)
        {           
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            _plane.SetNormalAndPosition(Vector3.up, Vector3.up*position.y);
            
            if (_plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return ScreenPointToRay(screenPoint).point;
        }
        
        public RaycastHit ScreenPointToRay(Vector2 screenPoint)
        {  
            Ray ray = _camera.ScreenPointToRay(screenPoint);

            Physics.Raycast(ray, out RaycastHit hit);
            
            return hit;
        }
    }
}