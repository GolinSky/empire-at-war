using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Services.Graphics
{
    public static class GraphicsUpscalerSupport
    {
        public static GraphicsUpscaler[] Available
        {
            get
            {
                var options = new List<GraphicsUpscaler> { GraphicsUpscaler.Bilinear };
                bool supportsStp = STP.IsSupported();
                if (supportsStp || SystemInfo.graphicsShaderLevel >= 45) options.Add(GraphicsUpscaler.Fsr1);
                if (supportsStp) options.Add(GraphicsUpscaler.Stp);
#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
                if (SupportsDlss()) options.Add(GraphicsUpscaler.Dlss);
#endif
                return options.ToArray();
            }
        }

#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
        private static bool SupportsDlss()
        {
            if (SystemInfo.graphicsDeviceVendorID != 0x10DE ||
                !UpscalerRegistry.s_RegisteredUpscalers.ContainsKey(typeof(DLSSIUpscaler)) ||
                !UnityEngine.NVIDIA.NVUnityPlugin.IsLoaded())
                return false;

            // Recreating an existing NVIDIA device invalidates URP's live DLSS context.
            var device = UnityEngine.NVIDIA.GraphicsDevice.device ?? UnityEngine.NVIDIA.GraphicsDevice.CreateGraphicsDevice();
            return device != null && device.IsFeatureAvailable(UnityEngine.NVIDIA.GraphicsDeviceFeature.DLSS);
        }
#endif

        public static string Label(GraphicsUpscaler upscaler) => upscaler switch
        {
            GraphicsUpscaler.Fsr1 => "FSR 1",
            GraphicsUpscaler.Stp => "STP",
            GraphicsUpscaler.Dlss => "DLSS",
            _ => "Bilinear",
        };
    }
}
