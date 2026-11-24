using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    public sealed class UnitIonStunRendererFeature : ScriptableRendererFeature
    {
        public const uint ION_STUN_RENDERING_LAYER = 1u << 30;

        private UnitIonStunRenderPass _pass;

        public override void Create()
        {
            _pass = new UnitIonStunRenderPass();
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
