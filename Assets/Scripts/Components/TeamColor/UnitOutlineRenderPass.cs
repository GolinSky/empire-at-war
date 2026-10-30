using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    public sealed class UnitOutlineRenderPass : ScriptableRenderPass
    {
        private const string PASS_NAME = "Ship Outlines";
        private static readonly ShaderTagId _outlinePassTag = new("ShipOutline");

        public UnitOutlineRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            profilingSampler = new ProfilingSampler(PASS_NAME);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var resources = frameData.Get<UniversalResourceData>();
            var drawingSettings = RenderingUtils.CreateDrawingSettings(_outlinePassTag, renderingData,
                cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
            {
                renderingLayerMask = UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER
            };
            var rendererListParams = new RendererListParams(renderingData.cullResults,
                drawingSettings, filteringSettings);

            using var builder = renderGraph.AddRasterRenderPass<PassData>(PASS_NAME,
                out var passData, profilingSampler);
            passData.RendererList = renderGraph.CreateRendererList(rendererListParams);
            builder.UseRendererList(passData.RendererList);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                context.cmd.DrawRendererList(data.RendererList));
        }

        private sealed class PassData
        {
            public RendererListHandle RendererList;
        }
    }
}
