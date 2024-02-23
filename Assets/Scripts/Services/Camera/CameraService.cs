using UnityEngine;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 GetWorldPoint(Vector2 screenPoint);
        RaycastHit ScreenPointToRay(Vector2 screenPoint);
        Vector3 CameraPosition { get; }
        float FieldOfView { get; }
    }

    public class CameraService : Service, ICameraService
    {
        private readonly UnityEngine.Camera camera;

        public Vector3 CameraPosition => camera.transform.position;
        public float FieldOfView => camera.fieldOfView;


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