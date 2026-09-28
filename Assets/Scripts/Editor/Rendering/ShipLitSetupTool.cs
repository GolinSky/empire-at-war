using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Squadrons;
using UnityEditor;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Moves unit prefabs onto the EmpireAtWar/Ship Lit shader and gives them team colors.
    /// Safe to run again after adding new units: converted materials and existing views are updated in place.
    /// </summary>
    public static class ShipLitSetupTool
    {
        private const string CONVERT_MENU_PATH = "Tools/Rendering/Convert Unit Materials To Ship Lit";
        private const string TEAM_COLOR_MENU_PATH = "Tools/Rendering/Add Team Color Views To Unit Prefabs";
        private const string SHIP_LIT_SHADER_PATH = "Assets/Art/Shaders/Units/ShipLit.shader";
        private const string LIT_SHADER_NAME = "Universal Render Pipeline/Lit";
        private const string COMPLEX_LIT_SHADER_NAME = "Universal Render Pipeline/Complex Lit";
        private const string MESH_RENDERERS_PROPERTY = "meshRenderers";
        private const float OPAQUE_SURFACE = 0f;

        private static readonly string[] UNIT_PREFAB_FOLDERS =
        {
            "Assets/Prefabs/Models/Ships",
            "Assets/Prefabs/Models/Stations",
            "Assets/Prefabs/Models/DefendStation",
            "Assets/Prefabs/Models/MiningFacilities",
            "Assets/Prefabs/Models/Squadrons"
        };

        private static readonly int BUMP_MAP_ID = Shader.PropertyToID("_BumpMap");
        private static readonly int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");
        private static readonly int METALLIC_ID = Shader.PropertyToID("_Metallic");
        private static readonly int SURFACE_ID = Shader.PropertyToID("_Surface");
        private static readonly int ALPHA_CLIP_ID = Shader.PropertyToID("_AlphaClip");

        [MenuItem(CONVERT_MENU_PATH)]
        public static void ConvertUnitMaterials()
        {
            Shader shipLit = AssetDatabase.LoadAssetAtPath<Shader>(SHIP_LIT_SHADER_PATH);
            HashSet<Material> materials = CollectUnitMaterials();
            StringBuilder report = new StringBuilder("[ShipLit] Material conversion\n");
            int alreadyConverted = materials.Count(material => material.shader == shipLit);
            int converted = AutodeskUnitMaterialConversion.Convert(materials, shipLit,
                FindUnitPrefabPaths().Where(path => IsUnitRoot(AssetDatabase.LoadAssetAtPath<GameObject>(path))), report);
            foreach (Material material in materials)
            {
                string shaderName = material.shader.name;
                if (material.shader == shipLit || shaderName == AutodeskMaterialConverter.SOURCE_SHADER_NAME) continue;
                if (shaderName != LIT_SHADER_NAME && shaderName != COMPLEX_LIT_SHADER_NAME)
                {
                    report.AppendLine($"  skipped (unsupported shader {shaderName}): {AssetDatabase.GetAssetPath(material)}");
                    continue;
                }

                // Ship Lit is opaque only; transparent or cut-out parts (glass, decals) keep their shader.
                if (material.GetFloat(SURFACE_ID) != OPAQUE_SURFACE || material.GetFloat(ALPHA_CLIP_ID) > 0f)
                {
                    report.AppendLine($"  skipped (transparent/cut-out): {AssetDatabase.GetAssetPath(material)}");
                    continue;
                }

                ConvertLitMaterial(material, shipLit);
                converted++;
            }

            AssetDatabase.SaveAssets();
            report.AppendLine($"  converted {converted} materials; skipped {alreadyConverted} already Ship Lit");
            Debug.Log(report.ToString());
        }

        [MenuItem(TEAM_COLOR_MENU_PATH)]
        public static void AddTeamColorViews()
        {
            StringBuilder report = new StringBuilder("[ShipLit] Team color views\n");
            foreach (string path in FindUnitPrefabPaths())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!IsUnitRoot(root))
                    {
                        continue;
                    }

                    TeamColorView view = root.GetComponent<TeamColorView>();
                    if (view == null)
                    {
                        view = root.AddComponent<TeamColorView>();
                    }

                    // Authoring-time collection only: the result is stored as explicit serialized references.
                    MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    SerializedObject serializedView = new SerializedObject(view);
                    SerializedProperty rendererList = serializedView.FindProperty(MESH_RENDERERS_PROPERTY);
                    rendererList.arraySize = renderers.Length;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        rendererList.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                    }

                    serializedView.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.AppendLine($"  {path}: {renderers.Length} renderers");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// URP/Lit ignores maps whose keyword is off; Ship Lit always samples them. These fix-ups keep
        /// each converted material looking the way it did under URP/Lit.
        /// </summary>
        private static void ConvertLitMaterial(Material material, Shader shipLit)
        {
            bool usesNormalMap = material.IsKeywordEnabled("_NORMALMAP");
            bool usesEmission = material.IsKeywordEnabled("_EMISSION");
            bool usesMetallicMap = material.IsKeywordEnabled("_METALLICSPECGLOSSMAP");

            material.shader = shipLit;
            material.shaderKeywords = new string[0];
            if (!usesNormalMap)
            {
                material.SetTexture(BUMP_MAP_ID, null);
            }

            if (!usesEmission)
            {
                material.SetColor(EMISSION_COLOR_ID, Color.black);
            }

            // With a metallic map URP/Lit reads metallic from the map only; Ship Lit multiplies map x value.
            if (usesMetallicMap)
            {
                material.SetFloat(METALLIC_ID, 1f);
            }

            EditorUtility.SetDirty(material);
        }

        private static HashSet<Material> CollectUnitMaterials()
        {
            HashSet<Material> materials = new HashSet<Material>();
            foreach (string path in FindUnitPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!IsUnitRoot(prefab))
                {
                    continue;
                }

                foreach (MeshRenderer meshRenderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    foreach (Material material in meshRenderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }

            return materials;
        }

        private static IEnumerable<string> FindUnitPrefabPaths()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", UNIT_PREFAB_FOLDERS))
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        // Placement ghosts and other helper prefabs live in the same folders; only real units get colors.
        private static bool IsUnitRoot(GameObject root)
        {
            return root.GetComponent<ShipEntity>() != null ||
                   root.GetComponent<Squadron>() != null ||
                   root.GetComponent<SpaceStationEntity>() != null ||
                   root.GetComponent<DefendPlatform>() != null ||
                   root.GetComponent<MiningFacilityEntity>() != null;
        }
    }
}
