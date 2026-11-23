using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EmpireAtWar.Components.TeamColor
{
    public sealed class UnitOutlineRenderPass : ScriptableRenderPass
    {
        private const string PASS_NAME = "Ship Outlines";
        private const string OWNER_PASS_NAME = "Ship Outline Owners";
        private static readonly ShaderTagId _outlineOwnerPassTag = new("ShipOutlineOwner");
        private static readonly ShaderTagId _outlinePassTag = new("ShipOutline");
        private static readonly int _ownerTextureId = Shader.PropertyToID("_UnitOutlineOwnerTexture");
        private readonly ProfilingSampler _ownerSampler = new(OWNER_PASS_NAME);

        public UnitOutlineRenderPass()
        {
            // Outlines do not write depth; the skybox and transparent nebula would overwrite
            // their pixels outside the hull if this pass ran before background rendering.
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
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

            // Per pixel, the owner id of the nearest highlighted unit. Unlike one shared stencil bit, it lets an
            // outline skip only its own silhouette, so a highlighted dreadnought cannot hide the ships above it.
            TextureDesc ownerDesc = renderGraph.GetTextureDesc(resources.activeColorTexture);
            ownerDesc.name = "_UnitOutlineOwners";
            ownerDesc.format = GraphicsFormat.R16_SFloat;
            ownerDesc.msaaSamples = MSAASamples.None;
            ownerDesc.bindTextureMS = false;
            ownerDesc.clearBuffer = true;
            ownerDesc.clearColor = Color.clear;
            TextureHandle owners = renderGraph.CreateTexture(ownerDesc);
            TextureDesc ownerDepthDesc = ownerDesc;
            ownerDepthDesc.name = "_UnitOutlineOwnerDepth";
            ownerDepthDesc.depthBufferBits = DepthBits.Depth32;
            TextureHandle ownerDepth = renderGraph.CreateTexture(ownerDepthDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(OWNER_PASS_NAME,
                       out var ownerData, _ownerSampler))
            {
                ownerData.Renderers = CreateRendererList(renderGraph, _outlineOwnerPassTag, renderingData,
                    cameraData, lightData, filteringSettings);
                builder.UseRendererList(ownerData.Renderers);
                builder.SetRenderAttachment(owners, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(ownerDepth, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(owners, _ownerTextureId);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(data.Renderers));
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PASS_NAME,
                       out var passData, profilingSampler))
            {
                passData.Renderers = CreateRendererList(renderGraph, _outlinePassTag, renderingData,
                    cameraData, lightData, filteringSettings);
                builder.UseRendererList(passData.Renderers);
                builder.UseTexture(owners);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(data.Renderers));
            }
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
            public RendererListHandle Renderers;
        }
    }
}
