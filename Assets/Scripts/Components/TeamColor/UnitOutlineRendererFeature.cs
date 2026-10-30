using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    public sealed class UnitOutlineRendererFeature : ScriptableRendererFeature
    {
        public const uint OUTLINE_RENDERING_LAYER = 1u << 31;

        private UnitOutlineRenderPass _pass;

        public override void Create()
        {
            _pass = new UnitOutlineRenderPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            renderer.EnqueuePass(_pass);
        }
    }
}
