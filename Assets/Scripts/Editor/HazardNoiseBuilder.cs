using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    public static class HazardNoiseBuilder
    {
        private const int SIZE = 1024;
        private const string NOISE_PATH = "Assets/Art/Textures/Vfx/HazardNoise.png";
        private const string WARP_PATH = "Assets/Art/Textures/Vfx/HazardWarp.png";
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/Vfx/";

        [MenuItem("Tools/Empire At War/Bake Hazard Noise")]
        public static void Build()
        {
            var material = new Material(Shader.Find("Hidden/EmpireAtWar/HazardNoiseBake"));
            try
            {
                Texture2D noise = Bake(material, NOISE_PATH, false);
                Texture2D warp = Bake(material, WARP_PATH, true);
                foreach (string name in new[] { "NebulaHazard", "IonStormHazard" })
                {
                    Material cloud = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_FOLDER + name + ".mat");
                    cloud.SetTexture("_Noise", noise);
                    cloud.SetTexture("_WarpNoise", warp);
                    EditorUtility.SetDirty(cloud);
                }
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(material); }
        }

        private static Texture2D Bake(Material material, string path, bool warp)
        {
            RenderTexture previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            var target = new RenderTexture(SIZE, SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false, true);
            try
            {
                material.SetFloat("_Warp", warp ? 1f : 0f);
                GL.sRGBWrite = false;
                Graphics.Blit(null, target, material);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgb;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
