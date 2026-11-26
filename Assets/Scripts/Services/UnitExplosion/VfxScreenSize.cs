using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>How large a world-space effect appears in the battle camera's output.</summary>
    public static class VfxScreenSize
    {
        /// <summary>On-screen diameter in pixels of an effect of <paramref name="diameter"/> world units at <paramref name="position"/>.</summary>
        public static float Pixels(UnityEngine.Camera camera, Vector3 position, float diameter)
        {
            Transform view = camera.transform;
            float viewHeight = camera.orthographic
                ? 2f * camera.orthographicSize
                : 2f * Mathf.Max(Vector3.Dot(position - view.position, view.forward), camera.nearClipPlane) *
                  Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return diameter / viewHeight * camera.pixelHeight;
        }
    }
}
