using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.ViewComponents.Health
{
    /// <summary>Copies the frame after transparents at half resolution, then draws shockwave spheres that sample it.</summary>
    public sealed class ShockwaveRefractionPass : ScriptableRenderPass
    {
        private const string COPY_PASS_NAME = "Shockwave Scene Copy";
        private const string DRAW_PASS_NAME = "Shockwave Refraction";
        private static readonly ShaderTagId _passTag = new("ShockwaveRefraction");
        private static readonly int _sceneColorId = Shader.PropertyToID("_ShockwaveSceneColor");

        public ShockwaveRefractionPass()
        {
            // After transparents, so lasers, fire and the nebula are refracted too; bloom later haloes the rim.
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler(DRAW_PASS_NAME);
            // The pass samples the camera color, so URP must not render straight into the back buffer.
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            // The back buffer cannot be sampled.
            if (resources.isActiveTargetBackBuffer) return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            // Half resolution: the bent image is soft anyway, and the copy costs a quarter of the pixels.
            descriptor.width = Mathf.Max(1, descriptor.width / 2);
            descriptor.height = Mathf.Max(1, descriptor.height / 2);
            TextureHandle sceneCopy = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor,
                "_ShockwaveSceneColor", false, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(COPY_PASS_NAME, out var passData,
                       profilingSampler))
            {
                passData.Source = resources.activeColorTexture;
                builder.UseTexture(passData.Source);
                builder.SetRenderAttachment(sceneCopy, 0, AccessFlags.WriteAll);
                builder.SetGlobalTextureAfterPass(sceneCopy, _sceneColorId);
                builder.SetRenderFunc(static (CopyPassData data, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1f, 1f, 0f, 0f), 0f, true));
            }

            using (var builder = renderGraph.AddRasterRenderPass<DrawPassData>(DRAW_PASS_NAME, out var passData,
                       profilingSampler))
            {
                var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(_passTag, renderingData,
                    cameraData, lightData, SortingCriteria.CommonTransparent);
                passData.Renderers = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                    drawingSettings, filteringSettings));
                builder.UseRendererList(passData.Renderers);
                builder.UseGlobalTexture(_sceneColorId);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc(static (DrawPassData data, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(data.Renderers));
            }
        }

        private sealed class CopyPassData
        {
            public TextureHandle Source;
        }

        private sealed class DrawPassData
        {
            public RendererListHandle Renderers;
        }
    }
}
