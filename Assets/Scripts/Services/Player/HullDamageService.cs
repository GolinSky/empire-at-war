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
        private static readonly int SOOT_COLOR_ID = Shader.PropertyToID("_HullDamageSootColor");
        private static readonly int BLISTER_COLOR_ID = Shader.PropertyToID("_HullDamageBlisterColor");
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
            // Every pixel a mark reaches (frayed edge along the surface, up to one radius deep) must lie within
            // one cell of its spot: the shader only searches the 3x3x3 cells around each pixel.
            float reach = Mathf.Sqrt((1f + settings.EdgeBreakup) * (1f + settings.EdgeBreakup) + 1f);
            float markRadius = Mathf.Min(settings.MarkSize, 1f / reach);
            Shader.SetGlobalVector(PARAMS_ID, new Vector4(1f / settings.DetailSize, 1f / settings.MarkSpacing,
                markRadius, settings.MarkDensity));
            Shader.SetGlobalVector(SURFACE_ID, new Vector4(settings.SootOpacity, settings.EdgeBreakup, 0f, 0f));
            Shader.SetGlobalVector(SOOT_COLOR_ID, settings.SootColor.linear);
            Shader.SetGlobalVector(BLISTER_COLOR_ID, settings.BlisterColor.linear);
        }

        public static void UploadQuality(int qualityLevel) => Shader.SetGlobalFloat(QUALITY_ID, qualityLevel);
    }
}
