using UnityEngine;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 GetWorldPoint(Vector2 screenPoint);
        Vector3 CameraPosition { get; }
    }

    public class CameraService : Service, ICameraService
    {
        private readonly UnityEngine.Camera camera;

        public Vector3 CameraPosition => camera.transform.position;

        
        public CameraService(UnityEngine.Camera camera)
        {
            this.camera = camera;
        }
        
        public Vector3 GetWorldPoint(Vector2 screenPoint)
        {  
            Physics.Raycast(camera.ScreenPointToRay(screenPoint), out RaycastHit hit);
            return hit.point;
        }
    }
}