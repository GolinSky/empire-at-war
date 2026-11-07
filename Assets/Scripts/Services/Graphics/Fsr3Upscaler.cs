#if ENABLE_UPSCALER_FRAMEWORK
using FidelityFX;
using FidelityFX.FSR3;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Fsr = FidelityFX.FSR3.Fsr3Upscaler;

namespace EmpireAtWar.Services.Graphics
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public sealed class Fsr3Upscaler : AbstractUpscaler
    {
        public const string UPSCALER_NAME = "FSR 3.1";
        private static readonly ProfilingSampler _sampler = new(UPSCALER_NAME);
        private readonly Fsr3UpscalerOptions _options;
        private int _jitterPhaseCount = 8;
        private Vector2 _jitter;

        static Fsr3Upscaler() => Register();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => UpscalerRegistry.Register<Fsr3Upscaler, Fsr3UpscalerOptions>(UPSCALER_NAME);

        public Fsr3Upscaler(Fsr3UpscalerOptions options) => _options = options;
        public override string GetName() => UPSCALER_NAME;
        public override UpscalerOptions GetOptions() => _options;
        public override bool IsTemporalUpscaler() => true;

        public static bool IsSupported()
        {
            if (!SystemInfo.supportsComputeShaders || SystemInfo.graphicsShaderLevel < 50) return false;
            GraphicsFormat[] formats =
            {
                GraphicsFormat.R8_UNorm, GraphicsFormat.R16_SFloat, GraphicsFormat.R16G16_SFloat,
                GraphicsFormat.R32_SFloat, GraphicsFormat.R32G32B32A32_SFloat,
                GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.R8G8B8A8_UNorm
            };
            foreach (var format in formats)
                if (!SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.LoadStore) ||
                    !SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Sample)) return false;
            return SystemInfo.IsFormatSupported(GraphicsFormat.R32_UInt, GraphicsFormatUsage.LoadStore) &&
                SystemInfo.IsFormatSupported(GraphicsFormat.R32G32_SFloat, GraphicsFormatUsage.Sample);
        }

        public override void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
        {
            _jitterPhaseCount = Fsr.GetJitterPhaseCount(preUpscaleResolution.x, postUpscaleResolution.x);
        }

        public override void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
        {
            Fsr.GetJitterOffset(out float x, out float y, frameIndex, _jitterPhaseCount);
            _jitter = jitter = new Vector2(x, y);
            allowScaling = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var io = frameData.Get<UpscalingIO>();
            var historyManager = frameData.Get<UniversalCameraData>().historyManager;
            historyManager.RequestAccess<Fsr3CameraHistory>();
            var history = historyManager.GetHistoryForWrite<Fsr3CameraHistory>();
            var flags = Fsr.InitializationFlags.EnableAutoExposure | Fsr.InitializationFlags.EnableDynamicResolution;
            if (io.hdrInput) flags |= Fsr.InitializationFlags.EnableHighDynamicRange;
            if (io.invertedDepth) flags |= Fsr.InitializationFlags.EnableDepthInverted;
            if (io.jitteredMotionVectors) flags |= Fsr.InitializationFlags.EnableMotionVectorsJitterCancellation;
            bool reset = history.Prepare(_options.Assets, io.postUpscaleResolution, io.preUpscaleResolution, flags, io.frameIndex);

            var descriptor = io.cameraColor.GetDescriptor(renderGraph);
            descriptor.width = io.postUpscaleResolution.x;
            descriptor.height = io.postUpscaleResolution.y;
            descriptor.format = GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.useDynamicScale = false;
            descriptor.enableRandomWrite = true;
            descriptor.clearBuffer = false;
            descriptor.name = "FSR3 Output";
            var output = renderGraph.CreateTexture(descriptor);
            descriptor.width = io.preUpscaleResolution.x;
            descriptor.height = io.preUpscaleResolution.y;
            descriptor.format = GraphicsFormat.R8_UNorm;
            descriptor.name = "FSR3 Reactive Mask";
            var reactive = renderGraph.CreateTexture(descriptor);
            var opaque = frameData.Get<UniversalResourceData>().cameraOpaqueTexture;

            using (var builder = renderGraph.AddUnsafePass<PassData>(UPSCALER_NAME, out var data, _sampler))
            {
                data.Context = history.Context;
                data.Color = io.cameraColor;
                data.Depth = io.cameraDepth;
                data.DepthElement = io.cameraDepth.GetDescriptor(renderGraph).depthBufferBits != DepthBits.None
                    ? RenderTextureSubElement.Depth : RenderTextureSubElement.Color;
                data.Motion = io.motionVectorColor;
                data.Opaque = opaque;
                data.Reactive = reactive;
                data.Output = output;
                float sign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1 : 1;
                Vector2 motionScale = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC
                    ? (Vector2)io.motionVectorTextureSize : Vector2.one;
                data.Dispatch = new Fsr.DispatchDescription
                {
                    RenderSize = io.preUpscaleResolution, UpscaleSize = io.postUpscaleResolution,
                    JitterOffset = _jitter, MotionVectorScale = sign * motionScale,
                    FrameTimeDelta = io.deltaTime, PreExposure = io.preExposureValue,
                    CameraNear = io.invertedDepth ? io.farClipPlane : io.nearClipPlane,
                    CameraFar = io.invertedDepth ? io.nearClipPlane : io.farClipPlane,
                    CameraFovAngleVertical = io.fieldOfViewDegrees * Mathf.Deg2Rad,
                    ViewSpaceToMetersFactor = 1, Reset = reset || io.resetHistory,
                    Exposure = ResourceView.Unassigned, TransparencyAndComposition = ResourceView.Unassigned
                };
                builder.UseTexture(data.Color);
                builder.UseTexture(data.Depth);
                builder.UseTexture(data.Motion);
                builder.UseTexture(opaque);
                builder.UseTexture(reactive, AccessFlags.Write);
                builder.UseTexture(output, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((PassData pass, UnsafeGraphContext context) =>
                {
                    var command = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    pass.Dispatch.Color = new ResourceView(new RenderTargetIdentifier((Texture)pass.Color));
                    pass.Dispatch.Depth = new ResourceView(new RenderTargetIdentifier((Texture)pass.Depth), pass.DepthElement);
                    pass.Dispatch.MotionVectors = new ResourceView(new RenderTargetIdentifier((Texture)pass.Motion));
                    pass.Dispatch.Output = new ResourceView(new RenderTargetIdentifier((Texture)pass.Output));
                    pass.Dispatch.Reactive = new ResourceView(new RenderTargetIdentifier((Texture)pass.Reactive));
                    pass.Context.GenerateReactiveMask(new Fsr.GenerateReactiveDescription
                    {
                        ColorOpaqueOnly = new ResourceView(new RenderTargetIdentifier((Texture)pass.Opaque)),
                        ColorPreUpscale = pass.Dispatch.Color, OutReactive = pass.Dispatch.Reactive,
                        RenderSize = pass.Dispatch.RenderSize
                    }, command);
                    pass.Context.Dispatch(pass.Dispatch, command);
                });
            }
            io.cameraColor = output;
        }

        private sealed class PassData
        {
            public Fsr3UpscalerContext Context;
            public Fsr.DispatchDescription Dispatch;
            public RenderTextureSubElement DepthElement;
            public TextureHandle Color, Depth, Motion, Opaque, Reactive, Output;
        }
    }
}
#endif
