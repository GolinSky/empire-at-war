using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    /// <summary>Adds the "ShipIonStun" pass of ion-disabled units on top of their lit hulls.</summary>
    public sealed class UnitIonStunRenderPass : ScriptableRenderPass
    {
        private const string PASS_NAME = "Ship Ion Stun";
        private static readonly ShaderTagId _ionStunPassTag = new("ShipIonStun");

        public UnitIonStunRenderPass()
        {
            // Before transparents, so shields and nebulae still layer over the arcs; bloom later haloes them.
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            profilingSampler = new ProfilingSampler(PASS_NAME);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var resources = frameData.Get<UniversalResourceData>();
            // Only stunned renderers are on this layer, so the pass costs nothing while nobody is stunned.
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
            {
                renderingLayerMask = UnitIonStunRendererFeature.ION_STUN_RENDERING_LAYER
            };

            using var builder = renderGraph.AddRasterRenderPass<PassData>(PASS_NAME, out var passData,
                profilingSampler);
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(_ionStunPassTag, renderingData,
                cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            passData.Renderers = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                drawingSettings, filteringSettings));
            builder.UseRendererList(passData.Renderers);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                context.cmd.DrawRendererList(data.Renderers));
        }

        private sealed class PassData
        {
            public RendererListHandle Renderers;
        }
    }
}
