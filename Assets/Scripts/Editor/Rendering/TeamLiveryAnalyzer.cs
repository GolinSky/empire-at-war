using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Finds the painted livery of every unit material (for example the Republic red stripes) and stores
    /// its hue on the material, so EmpireAtWar/Ship Lit repaints that livery in the owner's team color.
    /// Saturated emissive lights are also switched to full team tint.
    /// </summary>
    public static class TeamLiveryAnalyzer
    {
        private const string MENU_PATH = "Tools/Rendering/Detect Team Livery Colors";
        private const int SAMPLE_SIZE = 256;
        private const int HUE_BINS = 36;
        // Matches the shader defaults; the shader reads the same saturation threshold.
        private const float MIN_SATURATION = 0.4f;
        private const float HUE_RANGE = 0.07f;
        // Too dark to read as paint in the lit hull.
        private const float MIN_VALUE = 0.04f;
        // The recolor is per pixel and only touches paint of the livery hue, so even small trim stripes qualify;
        // below this the few saturated pixels are compression noise.
        private const float MIN_LIVERY_FRACTION = 0.005f;
        private const float SATURATED_EMISSION = 0.5f;

        private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");
        private static readonly int LIVERY_HUE_ID = Shader.PropertyToID("_TeamLiveryHue");
        private static readonly int LIVERY_HUE_RANGE_ID = Shader.PropertyToID("_TeamLiveryHueRange");
        private static readonly int LIVERY_MIN_SATURATION_ID = Shader.PropertyToID("_TeamLiveryMinSaturation");
        private static readonly int LIVERY_STRENGTH_ID = Shader.PropertyToID("_TeamLiveryStrength");
        private static readonly int EMISSION_TINT_ID = Shader.PropertyToID("_TeamEmissionTint");

        [MenuItem(MENU_PATH)]
        public static void DetectUnitLiveries()
        {
            Shader shipLit = AssetDatabase.LoadAssetAtPath<Shader>(ShipLitSetupTool.SHIP_LIT_SHADER_PATH);
            StringBuilder report = new StringBuilder("[ShipLit] Team livery detection\n");
            foreach (Material material in ShipLitSetupTool.CollectUnitMaterials())
            {
                if (material.shader != shipLit)
                {
                    continue;
                }

                report.AppendLine($"  {AssetDatabase.GetAssetPath(material)}: {Analyze(material)}");
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        private static string Analyze(Material material)
        {
            Color[] pixels = ReadLinearAlbedo(material);
            float[] hueWeights = new float[HUE_BINS];
            List<float> liveryHues = new List<float>();
            foreach (Color pixel in pixels)
            {
                Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
                if (saturation < MIN_SATURATION || value < MIN_VALUE)
                {
                    continue;
                }

                hueWeights[Mathf.Min((int)(hue * HUE_BINS), HUE_BINS - 1)] += 1f;
                liveryHues.Add(hue);
            }

            float fraction = (float)liveryHues.Count / pixels.Length;
            bool hasLivery = fraction >= MIN_LIVERY_FRACTION;
            float liveryHue = hasLivery ? FindDominantHue(hueWeights, liveryHues) : 0f;
            material.SetFloat(LIVERY_HUE_ID, liveryHue);
            material.SetFloat(LIVERY_HUE_RANGE_ID, HUE_RANGE);
            material.SetFloat(LIVERY_MIN_SATURATION_ID, MIN_SATURATION);
            material.SetFloat(LIVERY_STRENGTH_ID, hasLivery ? 1f : 0f);

            // Colored lights (red beacons, blue engines) take the team color; white windows keep a partial tint.
            Color.RGBToHSV(material.GetColor(EMISSION_COLOR_ID).linear, out _, out float emissionSaturation, out _);
            bool hasColoredEmission = emissionSaturation >= SATURATED_EMISSION;
            if (hasColoredEmission)
            {
                material.SetFloat(EMISSION_TINT_ID, 1f);
            }

            return hasLivery
                ? $"livery hue {liveryHue:0.000} on {fraction:P0} of the albedo{(hasColoredEmission ? ", colored emission" : string.Empty)}"
                : $"no livery ({fraction:P1}){(hasColoredEmission ? ", colored emission" : string.Empty)}";
        }

        // Circular mean of the hues around the strongest histogram bin, so reds on both sides of 0 average correctly.
        private static float FindDominantHue(float[] hueWeights, List<float> hues)
        {
            int peak = 0;
            for (int i = 1; i < hueWeights.Length; i++)
            {
                if (hueWeights[i] > hueWeights[peak])
                {
                    peak = i;
                }
            }

            float peakHue = (peak + 0.5f) / HUE_BINS;
            Vector2 sum = Vector2.zero;
            foreach (float hue in hues)
            {
                float distance = Mathf.Abs(hue - peakHue);
                if (Mathf.Min(distance, 1f - distance) > HUE_RANGE)
                {
                    continue;
                }

                float angle = hue * Mathf.PI * 2f;
                sum += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            float meanHue = Mathf.Atan2(sum.y, sum.x) / (Mathf.PI * 2f);
            return meanHue < 0f ? meanHue + 1f : meanHue;
        }

        // The shader measures hue on linear colors, so the texture is read back in linear space
        // (a Linear render texture converts sRGB textures on sampling). Unreadable textures work too.
        private static Color[] ReadLinearAlbedo(Material material)
        {
            Color baseColor = material.GetColor(BASE_COLOR_ID).linear;
            Texture baseMap = material.GetTexture(BASE_MAP_ID);
            if (baseMap == null)
            {
                return new[] { baseColor };
            }

            RenderTexture target = RenderTexture.GetTemporary(
                SAMPLE_SIZE, SAMPLE_SIZE, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Texture2D readback = new Texture2D(SAMPLE_SIZE, SAMPLE_SIZE, TextureFormat.RGBAFloat, false, true);
            try
            {
                Graphics.Blit(baseMap, target);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, SAMPLE_SIZE, SAMPLE_SIZE), 0, 0);
                Color[] pixels = readback.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] *= baseColor;
                }

                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(readback);
            }
        }
    }
}
