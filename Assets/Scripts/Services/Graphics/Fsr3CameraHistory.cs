#if ENABLE_UPSCALER_FRAMEWORK
using FidelityFX.FSR3;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Fsr = FidelityFX.FSR3.Fsr3Upscaler;

namespace EmpireAtWar.Services.Graphics
{
    public sealed class Fsr3CameraHistory : CameraHistoryItem
    {
        public Fsr3UpscalerContext Context { get; private set; }
        private Fsr3UpscalerShaders _shaders;
        private Fsr3UpscalerAssets _assets;
        private Vector2Int _displaySize;
        private Vector2Int _renderSize;
        private Fsr.InitializationFlags _flags;
        private int _lastFrame = -1;

        public bool Prepare(Fsr3UpscalerAssets assets, Vector2Int displaySize, Vector2Int renderSize,
            Fsr.InitializationFlags flags, int frameIndex)
        {
            bool recreate = Context == null || _assets != assets || _displaySize != displaySize || _flags != flags;
            bool reset = recreate || _renderSize != renderSize || _lastFrame != frameIndex - 1;
            if (recreate)
            {
                Reset();
                _assets = assets;
                _displaySize = displaySize;
                _flags = flags;
                _shaders = assets.shaders.DeepCopy();
                Context = new Fsr3UpscalerContext();
                try
                {
                    Context.Create(new Fsr.ContextDescription
                    {
                        Flags = flags, MaxRenderSize = displaySize, MaxUpscaleSize = displaySize, Shaders = _shaders
                    });
                }
                catch
                {
                    Reset();
                    throw;
                }
            }
            _renderSize = renderSize;
            _lastFrame = frameIndex;
            return reset;
        }

        public override void Reset()
        {
            if (Context != null)
            {
                Context.Destroy();
                Context = null;
            }
            if (_shaders != null)
            {
                CoreUtils.Destroy(_shaders.prepareInputsPass);
                CoreUtils.Destroy(_shaders.lumaPyramidPass);
                CoreUtils.Destroy(_shaders.shadingChangePyramidPass);
                CoreUtils.Destroy(_shaders.shadingChangePass);
                CoreUtils.Destroy(_shaders.prepareReactivityPass);
                CoreUtils.Destroy(_shaders.lumaInstabilityPass);
                CoreUtils.Destroy(_shaders.accumulatePass);
                CoreUtils.Destroy(_shaders.sharpenPass);
                CoreUtils.Destroy(_shaders.autoGenReactivePass);
                CoreUtils.Destroy(_shaders.tcrAutoGenPass);
                CoreUtils.Destroy(_shaders.debugViewPass);
                _shaders = null;
            }
            _assets = null;
            _lastFrame = -1;
        }
    }
}
#endif
