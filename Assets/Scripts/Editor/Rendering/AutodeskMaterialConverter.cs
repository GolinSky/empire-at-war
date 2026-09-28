using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    public static class AutodeskMaterialConverter
    {
        public const string SOURCE_SHADER_NAME = "Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive";
        private const int OCCLUSION_CHANNEL = 0;

        public static void Convert(Material material, Shader shipLit)
        {
            if (material.shader == shipLit) return;
            if (material.shader.name != SOURCE_SHADER_NAME)
                throw new InvalidOperationException($"Unsupported Autodesk material: {AssetDatabase.GetAssetPath(material)}");

            bool useColor = material.GetFloat("_UseColorMap") > 0.5f;
            bool useMetallic = material.GetFloat("_UseMetallicMap") > 0.5f;
            bool useRoughness = material.GetFloat("_UseRoughnessMap") > 0.5f;
            bool useEmission = material.GetFloat("_UseEmissiveMap") > 0.5f;
            Texture color = useColor ? material.GetTexture("_MainTex") : null;
            Texture metallic = useMetallic ? material.GetTexture("_MetallicGlossMap") : null;
            Texture roughness = useRoughness ? material.GetTexture("_SpecGlossMap") : null;
            Texture normal = material.GetFloat("_UseNormalMap") > 0.5f ? material.GetTexture("_BumpMap") : null;
            // The Autodesk graph samples AO unconditionally; its exposed _UseAoMap toggle is unused.
            Texture occlusion = material.GetTexture("_OcclusionMap");
            Texture emission = useEmission ? material.GetTexture("_EmissionMap") : null;
            Color baseColor = useColor ? Color.white : material.GetColor("_Color");
            Color emissionColor = useEmission ? Color.white : material.GetColor("_EmissionColor");
            Vector4 tiling = material.GetVector("_UvTiling");
            Vector4 offset = material.GetVector("_UvOffset");
            int width = Mathf.Max(metallic != null ? metallic.width : 1, roughness != null ? roughness.width : 1);
            int height = Mathf.Max(metallic != null ? metallic.height : 1, roughness != null ? roughness.height : 1);
            Color[] metallicPixels = useMetallic ? ReadPixels(metallic != null ? metallic : Texture2D.whiteTexture, width, height) : null;
            Color[] roughnessPixels = useRoughness ? ReadPixels(roughness != null ? roughness : Texture2D.whiteTexture, width, height) : null;
            Color32[] packed = MetallicSmoothnessPacker.Pack(metallicPixels, roughnessPixels,
                material.GetFloat("_Metallic"), material.GetFloat("_Glossiness"), useMetallic, useRoughness);
            Texture source = metallic != null ? metallic : roughness != null ? roughness : color;
            string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source != null ? source : material));
            string prefix = Path.Combine(directory, Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(material))).Replace('\\', '/');
            Texture2D mask = SaveTexture(prefix + "_MetallicSmoothness.png", packed, width, height);
            Texture2D ao = null;
            if (occlusion != null)
            {
                Color[] pixels = ReadPixels(occlusion, occlusion.width, occlusion.height);
                Color32[] aoPixels = new Color32[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                    aoPixels[i] = new Color(0f, pixels[i][OCCLUSION_CHANNEL], 0f, 1f);
                ao = SaveTexture(prefix + "_Occlusion.png", aoPixels, occlusion.width, occlusion.height);
            }

            material.shader = shipLit;
            material.shaderKeywords = Array.Empty<string>();
            material.renderQueue = -1;
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", baseColor);
            material.SetTextureScale("_BaseMap", new Vector2(tiling.x, tiling.y));
            material.SetTextureOffset("_BaseMap", new Vector2(offset.x, offset.y));
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_OcclusionMap", ao);
            material.SetFloat("_OcclusionStrength", 1f);
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", emissionColor);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        private static Color[] ReadPixels(Texture source, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            bool previousSrgbWrite = GL.sRGBWrite;
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Texture2D readable = null;
            try
            {
                // Preserve the values Autodesk samples, including each source importer's sRGB decoding.
                GL.sRGBWrite = false;
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
                readable.wrapMode = source.wrapMode;
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                if (source.width == width && source.height == height) return readable.GetPixels();

                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        pixels[y * width + x] = readable.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgbWrite;
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static Texture2D SaveTexture(string path, Color32[] pixels, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = Mathf.Max(32, Mathf.NextPowerOfTwo(Mathf.Max(width, height)));
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
