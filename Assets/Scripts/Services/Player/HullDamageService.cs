using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    /// <summary>
    /// Uploads the shared hull damage look (ShipHullDamage.hlsl) once per battle, and the quality level whenever it
    /// changes. Per-unit damage travels in the renderer user value (UnitRenderStateComponent).
    /// </summary>
    public sealed class HullDamageService : IInitializable, ILateDisposable
    {
        private static readonly int PARAMS_ID = Shader.PropertyToID("_HullDamageParams");
        private static readonly int SURFACE_ID = Shader.PropertyToID("_HullDamageSurface");
        private static readonly int CORE_ID = Shader.PropertyToID("_HullDamageCore");
        private static readonly int SOOT_COLOR_ID = Shader.PropertyToID("_HullDamageSootColor");
        private static readonly int EMBER_COLOR_ID = Shader.PropertyToID("_HullDamageEmberColor");
        private static readonly int BURNT_COLOR_ID = Shader.PropertyToID("_HullDamageBurntColor");
        private static readonly int BURNT_ID = Shader.PropertyToID("_HullDamageBurnt");
        private static readonly int QUALITY_ID = Shader.PropertyToID("_HullDamageQuality");

        private readonly HullDamageSettings _settings;

        public HullDamageService(HullDamageSettings settings)
        {
            _settings = settings;
        }

        public void Initialize()
        {
            Upload(_settings);
            UploadQuality(QualitySettings.GetQualityLevel());
            QualitySettings.activeQualityLevelChanged += HandleQualityChanged;
        }

        public void LateDispose()
        {
            QualitySettings.activeQualityLevelChanged -= HandleQualityChanged;
        }

        private static void HandleQualityChanged(int previousLevel, int currentLevel) => UploadQuality(currentLevel);

        /// <summary>Also used by the editor preview, which has no battle to run this service.</summary>
        public static void Upload(HullDamageSettings settings)
        {
            // Every pixel a mark reaches (frayed edge along the surface, up to one radius deep) must lie within
            // one cell of its spot: the shader only searches the 3x3x3 cells around each pixel. The tightest
            // spacing that still fits the largest mark gives the most marks.
            float reach = Mathf.Sqrt((1f + settings.EdgeBreakup) * (1f + settings.EdgeBreakup) + 1f);
            float spacing = settings.MarkRadiusMax * reach;
            Shader.SetGlobalVector(PARAMS_ID, new Vector4(settings.EdgeNoiseFrequency, 1f / spacing, 1f / reach,
                settings.MarkDensity));
            Shader.SetGlobalVector(SURFACE_ID, new Vector4(settings.SootOpacityMin, settings.EdgeBreakup,
                Mathf.Clamp01(settings.MarkRadiusMin / settings.MarkRadiusMax), settings.SootOpacityMax));
            Shader.SetGlobalVector(CORE_ID, new Vector4(settings.SootOpacityEdge, settings.StarStrength,
                settings.StarMinSize, settings.EmberLength));
            Shader.SetGlobalVector(SOOT_COLOR_ID, settings.SootColor.linear);
            Vector4 ember = settings.EmberColor.linear * settings.EmberStrength;
            ember.w = settings.EmberWidth;
            Shader.SetGlobalVector(EMBER_COLOR_ID, ember);
            Shader.SetGlobalVector(BURNT_COLOR_ID, settings.BurntColor.linear);
            Shader.SetGlobalVector(BURNT_ID, new Vector4(settings.BurntOffset, settings.BurntDepth,
                settings.DetailMinPixels, settings.FarSootOpacity));
        }

        /// <summary>Quality level as the shader reads it; also used by the editor preview.</summary>
        public static void UploadQuality(int qualityLevel) => Shader.SetGlobalFloat(QUALITY_ID, qualityLevel);
    }
}
