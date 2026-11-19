using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    public static class CameraViewportExtensions
    {
        public static bool IsOnScreen(this ICameraService camera, Vector3 worldPoint)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPoint);
            return viewportPoint.z > 0f &&
                   viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                   viewportPoint.y >= 0f && viewportPoint.y <= 1f;
        }
    }
}
