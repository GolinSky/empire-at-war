using UnityEngine;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 GetWorldPoint(Vector2 screenPoint);
        RaycastHit ScreenPointToRay(Vector2 screenPoint);
        Vector3 CameraPosition { get; }
        Vector3 CameraForward { get; }
        float FieldOfView { get; }
        Vector3 WorldToViewportPoint(Vector3 currentPosition);
        Vector2 WorldToScreenPoint(Vector3 position);
    }

    public class CameraService : Service, ICameraService
    {
        private readonly UnityEngine.Camera camera;

        public Vector3 CameraPosition => camera.transform.position;
        public Vector3 CameraForward => camera.transform.forward;
        public float FieldOfView => camera.fieldOfView;
        public Vector3 WorldToViewportPoint(Vector3 currentPosition)
        {
            return camera.WorldToViewportPoint(currentPosition);
        }

        public Vector2 WorldToScreenPoint(Vector3 position)
        {
            return camera.WorldToScreenPoint(position);
        }

        public CameraService(UnityEngine.Camera camera)
        {
            this.camera = camera;
        }
        
        public Vector3 GetWorldPoint(Vector2 screenPoint)
        {  
            Physics.Raycast(camera.ScreenPointToRay(screenPoint), out RaycastHit hit);
            return hit.point;
        }
        
        public RaycastHit ScreenPointToRay(Vector2 screenPoint)
        {  
            Physics.Raycast(camera.ScreenPointToRay(screenPoint), out RaycastHit hit);
            return hit;
        }
    }
}