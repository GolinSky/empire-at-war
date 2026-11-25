using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class ShockwaveRefractionFeature : ScriptableRendererFeature
    {
        private ShockwaveRefractionPass _pass;

        public override void Create()
        {
            _pass = new ShockwaveRefractionPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // The frame copy is a full-screen blit, so it runs only while a shockwave is alive.
            if (ShockwaveView.ActiveCount == 0) return;
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection ||
                UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            renderer.EnqueuePass(_pass);
        }
    }
}
