using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    /// <summary>
    /// Uploads the shared hull damage look (ShipHullDamage.hlsl) once per battle, and the quality level
    /// whenever it changes. Per-unit damage travels in the renderer user value (UnitRenderStateComponent).
    /// </summary>
    public sealed class HullDamageService : IInitializable, ITickable
    {
        private static readonly int MAP_ID = Shader.PropertyToID("_HullDamageMap");
        private static readonly int PARAMS_ID = Shader.PropertyToID("_HullDamageParams");
        private static readonly int SURFACE_ID = Shader.PropertyToID("_HullDamageSurface");
        private static readonly int CHAR_COLOR_ID = Shader.PropertyToID("_HullDamageCharColor");
        private static readonly int METAL_COLOR_ID = Shader.PropertyToID("_HullDamageMetalColor");
        private static readonly int EMBER_COLOR_ID = Shader.PropertyToID("_HullDamageEmberColor");
        private static readonly int QUALITY_ID = Shader.PropertyToID("_HullDamageQuality");

        private readonly HullDamageSettings _settings;
        private int _qualityLevel = -1;

        public HullDamageService(HullDamageSettings settings)
        {
            _settings = settings;
        }

        public void Initialize()
        {
            Upload(_settings);
            Tick();
        }

        public void Tick()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            if (_qualityLevel == qualityLevel) return;
            _qualityLevel = qualityLevel;
            UploadQuality(qualityLevel);
        }

        /// <summary>Also used by the editor preview, which has no battle to run this service.</summary>
        public static void Upload(HullDamageSettings settings)
        {
            Shader.SetGlobalTexture(MAP_ID, settings.DetailMap);
            Shader.SetGlobalVector(PARAMS_ID, new Vector4(1f / settings.DetailSize, 1f / settings.PatchSize,
                settings.EdgeWidth, settings.HeightBlend));
            Shader.SetGlobalVector(SURFACE_ID, new Vector4(settings.NormalStrength, settings.EmberStrength,
                settings.Coverage, settings.EmberFlickerSpeed));
            Shader.SetGlobalVector(CHAR_COLOR_ID, settings.CharColor.linear);
            Shader.SetGlobalVector(METAL_COLOR_ID, settings.MetalColor.linear);
            Shader.SetGlobalVector(EMBER_COLOR_ID, settings.EmberColor.linear);
        }

        public static void UploadQuality(int qualityLevel) => Shader.SetGlobalFloat(QUALITY_ID, qualityLevel);
    }
}
