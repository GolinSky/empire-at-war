using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature {

    [System.Serializable]
    private class ScreenSpaceOutlineSettings {

        [Header("General Outline Settings")]
        public Color outlineColor = Color.black;
        [Range(0.0f, 20.0f)]
        public float outlineScale = 1.0f;

        [Header("Depth Settings")]
        [Range(0.0f, 100.0f)]
        public float depthThreshold = 1.5f;
        [Range(0.0f, 500.0f)]
        public float robertsCrossMultiplier = 100.0f;

        [Header("Normal Settings")]
        [Range(0.0f, 1.0f)]
        public float normalThreshold = 0.4f;

        [Header("Depth Normal Relation Settings")]
        [Range(0.0f, 2.0f)]
        public float steepAngleThreshold = 0.2f;
        [Range(0.0f, 500.0f)]
        public float steepAngleMultiplier = 25.0f;

        [Header("General Scene View Space Normal Texture Settings")]
        public RenderTextureFormat colorFormat;
        public int depthBufferBits;
        public FilterMode filterMode;
        public Color backgroundColor = Color.clear;

        [Header("View Space Normal Texture Object Draw Settings")]
        public PerObjectData perObjectData;
        public bool enableDynamicBatching;
        public bool enableInstancing;
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass {

        private static readonly int SceneViewSpaceNormalsId = Shader.PropertyToID("_SceneViewSpaceNormals");

        private readonly Material screenSpaceOutlineMaterial;
        private readonly ScreenSpaceOutlineSettings settings;
        private readonly FilteringSettings filteringSettings;
        private readonly List<ShaderTagId> shaderTagIdList;
        private readonly Material normalsMaterial;

        private class NormalsPassData {
            public RendererListHandle rendererList;
            public Color backgroundColor;
        }

        private class OutlinePassData {
            public TextureHandle source;
            public Material material;
        }

        public bool IsValid => screenSpaceOutlineMaterial != null && normalsMaterial != null;

        public ScreenSpaceOutlinePass(RenderPassEvent renderPassEvent, LayerMask layerMask,
            ScreenSpaceOutlineSettings settings) {
            this.settings = settings;
            this.renderPassEvent = renderPassEvent;

            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);

            screenSpaceOutlineMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Outlines"));
            screenSpaceOutlineMaterial.SetColor("_OutlineColor", settings.outlineColor);
            screenSpaceOutlineMaterial.SetFloat("_OutlineScale", settings.outlineScale);
            screenSpaceOutlineMaterial.SetFloat("_DepthThreshold", settings.depthThreshold);
            screenSpaceOutlineMaterial.SetFloat("_RobertsCrossMultiplier", settings.robertsCrossMultiplier);
            screenSpaceOutlineMaterial.SetFloat("_NormalThreshold", settings.normalThreshold);
            screenSpaceOutlineMaterial.SetFloat("_SteepAngleThreshold", settings.steepAngleThreshold);
            screenSpaceOutlineMaterial.SetFloat("_SteepAngleMultiplier", settings.steepAngleMultiplier);

            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

            shaderTagIdList = new List<ShaderTagId> {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            normalsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/ViewSpaceNormals"));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            if (!IsValid)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            RenderTextureDescriptor normalsDescriptor = cameraData.cameraTargetDescriptor;
            normalsDescriptor.colorFormat = settings.colorFormat;
            normalsDescriptor.depthBufferBits = 0;
            normalsDescriptor.msaaSamples = 1;

            TextureHandle normals = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                normalsDescriptor,
                "_SceneViewSpaceNormals",
                false,
                settings.filterMode);

            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                shaderTagIdList,
                renderingData,
                cameraData,
                lightData,
                cameraData.defaultOpaqueSortFlags);
            drawSettings.perObjectData = settings.perObjectData;
            drawSettings.enableDynamicBatching = settings.enableDynamicBatching;
            drawSettings.enableInstancing = settings.enableInstancing;
            drawSettings.overrideMaterial = normalsMaterial;

            RendererListParams rendererListParams = new RendererListParams(
                renderingData.cullResults,
                drawSettings,
                filteringSettings);
            RendererListHandle rendererList = renderGraph.CreateRendererList(rendererListParams);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<NormalsPassData>(
                "Screen Space Outline Normals",
                out NormalsPassData passData)) {
                passData.rendererList = rendererList;
                passData.backgroundColor = settings.backgroundColor;

                builder.UseRendererList(rendererList);
                builder.SetRenderAttachment(normals, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(normals, SceneViewSpaceNormalsId);
                builder.SetRenderFunc(static (NormalsPassData data, RasterGraphContext context) => {
                    context.cmd.ClearRenderTarget(RTClearFlags.Color, data.backgroundColor, 1.0f, 0);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
            destinationDescriptor.name = "CameraColor-ScreenSpaceOutlines";
            destinationDescriptor.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<OutlinePassData>(
                "Screen Space Outlines",
                out OutlinePassData passData)) {
                passData.source = source;
                passData.material = screenSpaceOutlineMaterial;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderFunc(static (OutlinePassData data, RasterGraphContext context) => {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            resourceData.cameraColor = destination;
        }

        public void Release() {
            CoreUtils.Destroy(screenSpaceOutlineMaterial);
            CoreUtils.Destroy(normalsMaterial);
        }
    }

    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
    [SerializeField] private LayerMask outlinesLayerMask;

    [SerializeField] private ScreenSpaceOutlineSettings outlineSettings = new ScreenSpaceOutlineSettings();

    private ScreenSpaceOutlinePass screenSpaceOutlinePass;

    public override void Create() {
        if (renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(renderPassEvent, outlinesLayerMask, outlineSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (screenSpaceOutlinePass != null && screenSpaceOutlinePass.IsValid)
            renderer.EnqueuePass(screenSpaceOutlinePass);
    }

    protected override void Dispose(bool disposing) {
        if (disposing)
            screenSpaceOutlinePass?.Release();
    }
}
