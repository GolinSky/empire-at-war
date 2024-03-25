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
        Vector3 CameraForward { get; }
        float FieldOfView { get; }
        Vector3 WorldToViewportPoint(Vector3 currentPosition);
        Vector2 WorldToScreenPoint(Vector3 position);
        void MoveTo(Vector3 worldPoint);
        void AddCommand(ICameraCommand cameraCommand);
    }

    public class CameraService : Service, ICameraService
    {
        private  ICameraCommand cameraCommand;
        private readonly UnityEngine.Camera camera;
        private Plane plane = new Plane();

        public Vector3 CameraPosition => camera.transform.position;
        public Vector3 CameraForward => camera.transform.forward;
        public float FieldOfView => camera.fieldOfView;

        public CameraService(UnityEngine.Camera camera)
        {
            this.camera = camera;
        }
        
        public Vector3 WorldToViewportPoint(Vector3 currentPosition)
        {
            return camera.WorldToViewportPoint(currentPosition);
        }

        public Vector2 WorldToScreenPoint(Vector3 position)
        {
            return camera.WorldToScreenPoint(position);
        }

        public void MoveTo(Vector3 worldPoint)
        {
            cameraCommand.MoveTo(worldPoint);
        }

        public void AddCommand(ICameraCommand cameraCommand)
        {
            this.cameraCommand = cameraCommand;
        }

        public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position)
        {           
            Ray ray = camera.ScreenPointToRay(screenPoint);
            plane.SetNormalAndPosition(Vector3.up, Vector3.up*position.y);
            
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return ScreenPointToRay(screenPoint).point;
        }
        
        public RaycastHit ScreenPointToRay(Vector2 screenPoint)
        {  
            Ray ray = camera.ScreenPointToRay(screenPoint);

            Physics.Raycast(ray, out RaycastHit hit);
            
            return hit;
        }
    }
}