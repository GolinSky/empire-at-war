using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor.Rendering
{
    public static class ParticleRecoveryBuilder
    {
        private const string PARTICLE_SHADER = "Universal Render Pipeline/Particles/Unlit";
        private const string COMPATIBILITY_SHADER = "EmpireAtWar/Vfx/Particle Legacy Unlit";
        private const string PPFX_FOLDER = "Assets/ThirdParty/ParticleProFX/Content/Materials/";

        private enum LegacyMode { Alpha, Additive, Premultiply, MultiplyDouble }

        // Original shader identity for the nine materials migrated before exact shader parity was added.
        private static readonly Dictionary<string, LegacyMode> _migratedMaterials = new Dictionary<string, LegacyMode>
        {
            { PPFX_FOLDER + "matppfx_default.mat", LegacyMode.Additive },
            { PPFX_FOLDER + "matppfx_explosion03.mat", LegacyMode.MultiplyDouble },
            { PPFX_FOLDER + "matppfx_explosion09.mat", LegacyMode.Alpha },
            { PPFX_FOLDER + "matppfx_fire01.mat", LegacyMode.Premultiply },
            { PPFX_FOLDER + "matppfx_shockwaveSheet.mat", LegacyMode.Additive },
            { PPFX_FOLDER + "matppfx_smoke02.mat", LegacyMode.Alpha },
            { PPFX_FOLDER + "matppfx_smoke03.mat", LegacyMode.Alpha },
            { PPFX_FOLDER + "matppfx_smoke07.mat", LegacyMode.Alpha },
            { "Assets/Art/Materials/Vfx/Nebula/NebulaMultiply.mat", LegacyMode.Premultiply }
        };

        [MenuItem("Tools/Rendering/Recover Particle Materials")]
        public static void Build()
        {
            Shader shader = Shader.Find(PARTICLE_SHADER);
            Shader compatibility = Shader.Find(COMPATIBILITY_SHADER);
            if (shader == null || compatibility == null) throw new InvalidOperationException("Missing particle recovery shaders.");
            var materials = new HashSet<Material>();
            int rendererCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string serialized = File.ReadAllText(path);
                if (!serialized.Contains("--- !u!199") && !serialized.Contains("propertyPath: m_CastShadows")) continue;
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool needsSave = false;
                foreach (UnityEngine.Object item in EditorUtility.CollectDeepHierarchy(new UnityEngine.Object[] { asset }))
                {
                    if (!(item is ParticleSystemRenderer renderer)) continue;
                    foreach (Material material in renderer.sharedMaterials)
                        if (material != null && (material.shader.name.StartsWith("Legacy Shaders/Particles/", StringComparison.Ordinal) ||
                            _migratedMaterials.ContainsKey(AssetDatabase.GetAssetPath(material)))) materials.Add(material);
                    if (renderer.shadowCastingMode != ShadowCastingMode.Off) needsSave = true;
                }
                if (!needsSave) continue;
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (UnityEngine.Object item in EditorUtility.CollectDeepHierarchy(new UnityEngine.Object[] { contents }))
                    {
                        if (!(item is ParticleSystemRenderer renderer)) continue;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        rendererCount++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            foreach (Material material in materials) Convert(material, shader, compatibility);
            AssetDatabase.SaveAssets();
            Debug.Log($"Particle recovery: saved {rendererCount} renderers without shadows; converted {materials.Count} materials.");
        }

        private static LegacyMode GetMode(Material material)
        {
            if (_migratedMaterials.TryGetValue(AssetDatabase.GetAssetPath(material), out LegacyMode mode)) return mode;
            switch (material.shader.name)
            {
                case "Legacy Shaders/Particles/Alpha Blended": return LegacyMode.Alpha;
                case "Legacy Shaders/Particles/Additive": return LegacyMode.Additive;
                case "Legacy Shaders/Particles/Alpha Blended Premultiply": return LegacyMode.Premultiply;
                case "Legacy Shaders/Particles/Multiply (Double)": return LegacyMode.MultiplyDouble;
                default: throw new InvalidOperationException($"Unsupported particle conversion: {material.shader.name}.");
            }
        }

        private static SerializedProperty SavedProperty(SerializedObject material, string collection, string name)
        {
            SerializedProperty entries = material.FindProperty("m_SavedProperties." + collection);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == name) return entry.FindPropertyRelative("second");
            }
            throw new InvalidOperationException($"Missing original particle property {name} on {material.targetObject.name}.");
        }

        private static void Convert(Material material, Shader shader, Shader compatibility)
        {
            LegacyMode mode = GetMode(material);
            var original = new SerializedObject(material);
            SerializedProperty textureProperty = SavedProperty(original, "m_TexEnvs", "_MainTex");
            Texture texture = (Texture)textureProperty.FindPropertyRelative("m_Texture").objectReferenceValue;
            Vector2 scale = textureProperty.FindPropertyRelative("m_Scale").vector2Value;
            Vector2 offset = textureProperty.FindPropertyRelative("m_Offset").vector2Value;
            Color tint = mode == LegacyMode.Alpha || mode == LegacyMode.Additive
                ? SavedProperty(original, "m_Colors", "_TintColor").colorValue : Color.white;
            float inverseFade = SavedProperty(original, "m_Floats", "_InvFade").floatValue;
            bool softParticles = material.IsKeywordEnabled("SOFTPARTICLES_ON") || material.IsKeywordEnabled("_SOFTPARTICLES_ON");
            int queue = material.renderQueue;
            bool multiply = mode == LegacyMode.MultiplyDouble;
            material.shader = multiply ? shader : compatibility;
            material.shaderKeywords = Array.Empty<string>();
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = queue >= 0 ? queue : (int)RenderQueue.Transparent;
            if (multiply)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
                // Legacy fragment doubles RGB, then DstColor/SrcColor doubles it again.
                material.SetColor("_BaseColor", new Color(4f, 4f, 4f, 1f));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 3f);
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_SoftParticlesEnabled", softParticles ? 1f : 0f);
                material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
                material.SetFloat("_SoftParticlesFarFadeDistance", 1f / inverseFade);
                material.SetVector("_SoftParticleFadeParams", new Vector4(0f, inverseFade, 0f, 0f));
                material.SetFloat("_FlipbookBlending", 0f);
                material.SetFloat("_ColorMode", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.DstColor);
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.Zero);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHAMODULATE_ON");
                if (softParticles) material.EnableKeyword("_SOFTPARTICLES_ON");
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.SetShaderPassEnabled("DepthOnly", false);
            }
            else
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
                material.SetColor("_TintColor", tint);
                material.SetFloat("_InvFade", inverseFade);
                material.SetFloat("_SrcBlend", (float)(mode == LegacyMode.Premultiply ? BlendMode.One : BlendMode.SrcAlpha));
                material.SetFloat("_DstBlend", (float)(mode == LegacyMode.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                if (mode == LegacyMode.Additive) material.EnableKeyword("_LEGACY_ADDITIVE");
                if (mode == LegacyMode.Premultiply) material.EnableKeyword("_LEGACY_PREMULTIPLY");
                if (softParticles) material.EnableKeyword("SOFTPARTICLES_ON");
            }
            EditorUtility.SetDirty(material);
        }
    }
}
