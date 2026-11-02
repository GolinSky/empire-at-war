using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    public sealed class UnitOutlineRenderPass : ScriptableRenderPass
    {
        private const string PASS_NAME = "Ship Outlines";
        private static readonly ShaderTagId _outlineMaskPassTag = new("ShipOutlineMask");
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
            // Both lists see only highlighted renderers, so unselected ships cost nothing here.
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
            {
                renderingLayerMask = UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER
            };

            using var builder = renderGraph.AddRasterRenderPass<PassData>(PASS_NAME,
                out var passData, profilingSampler);
            passData.MaskList = CreateRendererList(renderGraph, _outlineMaskPassTag, renderingData,
                cameraData, lightData, filteringSettings);
            passData.OutlineList = CreateRendererList(renderGraph, _outlinePassTag, renderingData,
                cameraData, lightData, filteringSettings);
            builder.UseRendererList(passData.MaskList);
            builder.UseRendererList(passData.OutlineList);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                // The mask stencils each highlighted silhouette before any outline is drawn.
                context.cmd.DrawRendererList(data.MaskList);
                context.cmd.DrawRendererList(data.OutlineList);
            });
        }

        private static RendererListHandle CreateRendererList(RenderGraph renderGraph, ShaderTagId passTag,
            UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData,
            FilteringSettings filteringSettings)
        {
            var drawingSettings = RenderingUtils.CreateDrawingSettings(passTag, renderingData,
                cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            return renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                drawingSettings, filteringSettings));
        }

        private sealed class PassData
        {
            public RendererListHandle MaskList;
            public RendererListHandle OutlineList;
        }
    }
}
