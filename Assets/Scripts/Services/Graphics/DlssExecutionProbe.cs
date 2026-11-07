#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
using UnityEngine;
using UnityEngine.NVIDIA;
using UnityEngine.Rendering;

namespace EmpireAtWar.Services.Graphics
{
    internal static class DlssExecutionProbe
    {
        private const int OUTPUT_WIDTH = 1280;
        private const int OUTPUT_HEIGHT = 720;

        // A poisoned NVIDIA session still reports feature support and returns a context.
        // Verify one known pixel before offering DLSS to the player.
        internal static bool CanRender(GraphicsDevice device)
        {
            if (!device.GetOptimalSettings(OUTPUT_WIDTH, OUTPUT_HEIGHT, DLSSQuality.MaximumQuality, out var optimal))
                return false;

            int width = (int)optimal.outRenderWidth;
            int height = (int)optimal.outRenderHeight;
            var color = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
            var depth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat);
            var motion = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RGHalf);
            var output = RenderTexture.GetTemporary(new RenderTextureDescriptor(OUTPUT_WIDTH, OUTPUT_HEIGHT,
                RenderTextureFormat.ARGBHalf, 0) { enableRandomWrite = true });
            var cmd = CommandBufferPool.Get("Check DLSS output");
            DLSSContext context = null;
            try
            {
                Clear(cmd, color, Color.red);
                Clear(cmd, depth, Color.white);
                Clear(cmd, motion, Color.clear);
                Clear(cmd, output, Color.black);
                var init = new DLSSCommandInitializationData
                {
                    inputRTWidth = (uint)width, inputRTHeight = (uint)height,
                    outputRTWidth = OUTPUT_WIDTH, outputRTHeight = OUTPUT_HEIGHT,
                    quality = DLSSQuality.MaximumQuality,
                };
                init.SetFlag(DLSSFeatureFlags.MVLowRes, true);
                init.SetFlag(DLSSFeatureFlags.IsHDR, true);
                context = device.CreateFeature(cmd, init);
                if (context == null) return false;
                context.executeData = new DLSSCommandExecutionData
                {
                    subrectWidth = (uint)width, subrectHeight = (uint)height,
                    reset = 1, preExposure = 1, mvScaleX = width, mvScaleY = height,
                };
                var textures = new DLSSTextureTable
                {
                    colorInput = color, colorOutput = output, depth = depth, motionVectors = motion,
                };
                device.ExecuteDLSS(cmd, context, textures);
                UnityEngine.Graphics.ExecuteCommandBuffer(cmd);
                var readback = AsyncGPUReadback.Request(output, 0, OUTPUT_WIDTH / 2, 1, OUTPUT_HEIGHT / 2, 1,
                    0, 1, TextureFormat.RGBA32);
                readback.WaitForCompletion();
                if (readback.hasError) return false;
                Color32 pixel = readback.GetData<Color32>()[0];
                return pixel.r > 128 && pixel.g < 64 && pixel.b < 64;
            }
            finally
            {
                if (context != null)
                {
                    cmd.Clear();
                    device.DestroyFeature(cmd, context);
                    UnityEngine.Graphics.ExecuteCommandBuffer(cmd);
                }
                CommandBufferPool.Release(cmd);
                RenderTexture.ReleaseTemporary(output);
                RenderTexture.ReleaseTemporary(motion);
                RenderTexture.ReleaseTemporary(depth);
                RenderTexture.ReleaseTemporary(color);
            }
        }

        private static void Clear(CommandBuffer cmd, RenderTexture target, Color color)
        {
            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(false, true, color);
        }
    }
}
#endif
