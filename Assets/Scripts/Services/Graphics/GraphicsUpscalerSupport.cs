using EmpireAtWar.Services.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Services.Graphics
{
    public static class GraphicsUpscalerSupport
    {
        public static GraphicsUpscaler[] Available => STP.IsSupported()
            ? new[] { GraphicsUpscaler.Bilinear, GraphicsUpscaler.Fsr1, GraphicsUpscaler.Stp }
            : SystemInfo.graphicsShaderLevel >= 45
                ? new[] { GraphicsUpscaler.Bilinear, GraphicsUpscaler.Fsr1 }
                : new[] { GraphicsUpscaler.Bilinear };

        public static string Label(GraphicsUpscaler upscaler) => upscaler switch
        {
            GraphicsUpscaler.Fsr1 => "FSR 1",
            GraphicsUpscaler.Stp => "STP",
            _ => "Bilinear",
        };
    }
}
