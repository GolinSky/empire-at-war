#if ENABLE_UPSCALER_FRAMEWORK
using FidelityFX.FSR3;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Services.Graphics
{
    public sealed class Fsr3UpscalerOptions : UpscalerOptions
    {
        [SerializeField] private Fsr3UpscalerAssets assets;
        public Fsr3UpscalerAssets Assets => assets;
    }
}
#endif
