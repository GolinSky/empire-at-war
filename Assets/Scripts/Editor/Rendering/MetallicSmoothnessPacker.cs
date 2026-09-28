using System;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    public static class MetallicSmoothnessPacker
    {
        private const int METALLIC_CHANNEL = 0;
        private const int ROUGHNESS_CHANNEL = 0;

        public static Color32 Pack(float metallic, float roughness)
        {
            // Autodesk Interactive converts both map and scalar roughness through Square Root.
            return new Color(metallic, 0f, 0f, 1f - Mathf.Sqrt(roughness));
        }

        public static Color32[] Pack(Color[] metallicPixels, Color[] roughnessPixels,
            float metallicFallback, float roughnessFallback, bool useMetallicMap, bool useRoughnessMap)
        {
            if (useMetallicMap && useRoughnessMap && metallicPixels.Length != roughnessPixels.Length)
            {
                throw new ArgumentException("Metallic and roughness maps must be resampled to the same size before packing.");
            }

            int count = useMetallicMap ? metallicPixels.Length : useRoughnessMap ? roughnessPixels.Length : 1;
            Color32[] packed = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                float metallic = useMetallicMap ? metallicPixels[i][METALLIC_CHANNEL] : metallicFallback;
                float roughness = useRoughnessMap ? roughnessPixels[i][ROUGHNESS_CHANNEL] : roughnessFallback;
                packed[i] = Pack(metallic, roughness);
            }

            return packed;
        }
    }
}
