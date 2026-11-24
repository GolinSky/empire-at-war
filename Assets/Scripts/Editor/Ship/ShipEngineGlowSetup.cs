using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship
{
    /// <summary>
    /// Moves the engine glow materials onto EngineGlow.shader and gives every ship view a
    /// <see cref="ShipEngineGlowComponent"/> listing its enabled glow renderers, also hidden by fog. Glow renderers
    /// leave UnitRenderStateComponent: the renderer user value carries the glow fade, so it has one writer.
    /// Rerunning rebuilds the lists from the current prefabs.
    /// </summary>
    public static class ShipEngineGlowSetup
    {
        public const string SHADER_PATH = "Assets/Art/Shaders/Units/EngineGlow.shader";
        public const string VIEW_FOLDER = "Assets/Prefabs/Models";
        public const string GLOW_RENDERERS_PROPERTY = "glowRenderers";

        public static readonly string[] GLOW_MATERIAL_PATHS =
        {
            "Assets/Art/Materials/Models/EmpireShips/VictoryII/VictoryII_P_ISD_ENG_Material.mat",
            "Assets/Art/Materials/Models/EmpireShips/VictoryI/VictoryI_P_VSD_ENG_Material.mat",
            "Assets/Art/Materials/Models/EmpireShips/VictoryI/VictoryI_EV_Victory_SD_I_Thruster_Material.mat"
        };

        private const string MENU_PATH = "Tools/Empire At War/Units/Ships/Set Up Engine Glow";
        private const string INJECTABLES_PROPERTY = "<Injectables>k__BackingField";
        private const string RENDER_STATE_RENDERERS_PROPERTY = "meshRenderers";
        private const string FOG_RENDERERS_PROPERTY = "renderers";
        private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int CULL_ID = Shader.PropertyToID("_Cull");

        [MenuItem(MENU_PATH)]
        public static void SetUpAll()
        {
            StringBuilder report = new StringBuilder("Engine glow setup:\n");
            ConvertMaterials(report);
            // Bases first: a variant (Devastator, Chimaera of ISD II) must inherit the base's component, not add a second.
            foreach (string path in FindShipViewPaths().OrderBy(VariantDepth)) report.AppendLine(Apply(path));
            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        public static bool UsesEngineGlow(Renderer renderer) =>
            renderer.sharedMaterials.Any(material => material != null && IsEngineGlow(material));

        public static IEnumerable<string> FindShipViewPaths() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { VIEW_FOLDER })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => IsShipView(AssetDatabase.LoadAssetAtPath<GameObject>(path)));

        public static bool IsShipView(GameObject prefab)
        {
            EntityBindings bindings = prefab.GetComponent<EntityBindings>();
            return bindings != null && bindings.ShipMove != null;
        }

        /// <summary>Keeps texture, tiling, tint and culling; everything else comes from the shader defaults.</summary>
        public static void ConvertMaterials(StringBuilder report)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
            foreach (string path in GLOW_MATERIAL_PATHS)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material.shader == shader) continue;
                Texture baseMap = material.GetTexture(BASE_MAP_ID);
                Vector2 scale = material.GetTextureScale(BASE_MAP_ID);
                Vector2 offset = material.GetTextureOffset(BASE_MAP_ID);
                Color color = material.GetColor(BASE_COLOR_ID);
                float cull = material.GetFloat(CULL_ID);
                material.shader = shader;
                material.shaderKeywords = new string[0];
                material.renderQueue = -1;
                material.SetTexture(BASE_MAP_ID, baseMap);
                material.SetTextureScale(BASE_MAP_ID, scale);
                material.SetTextureOffset(BASE_MAP_ID, offset);
                material.SetColor(BASE_COLOR_ID, color);
                material.SetFloat(CULL_ID, cull);
                EditorUtility.SetDirty(material);
                report.AppendLine("  converted " + path);
            }
        }

        public static string Apply(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ShipEngineGlowComponent engineGlow = root.GetComponent<ShipEngineGlowComponent>();
                if (engineGlow == null) engineGlow = root.AddComponent<ShipEngineGlowComponent>();

                MeshRenderer[] glows = root.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(renderer => renderer.enabled && UsesEngineGlow(renderer)).ToArray();
                SerializedObject serializedGlow = new SerializedObject(engineGlow);
                SetArray(serializedGlow.FindProperty(GLOW_RENDERERS_PROPERTY), glows);
                serializedGlow.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject bindings = new SerializedObject(root.GetComponent<EntityBindings>());
                SerializedProperty injectables = bindings.FindProperty(INJECTABLES_PROPERTY);
                List<Object> injected = Read(injectables);
                if (!injected.Contains(engineGlow))
                {
                    injected.Add(engineGlow);
                    SetArray(injectables, injected);
                    bindings.ApplyModifiedPropertiesWithoutUndo();
                }

                // Fog hides glows with the hull (forceRenderingOff), independent of the user value.
                foreach (FogVisibilityComponent fog in root.GetComponentsInChildren<FogVisibilityComponent>(true))
                {
                    SerializedObject serializedFog = new SerializedObject(fog);
                    SerializedProperty fogRenderers = serializedFog.FindProperty(FOG_RENDERERS_PROPERTY);
                    List<Object> hidden = Read(fogRenderers);
                    hidden.AddRange(glows.Where(glow => !hidden.Contains(glow)));
                    SetArray(fogRenderers, hidden);
                    serializedFog.ApplyModifiedPropertiesWithoutUndo();
                }

                int removed = 0;
                foreach (UnitRenderStateComponent renderState in root.GetComponentsInChildren<UnitRenderStateComponent>(true))
                {
                    SerializedObject serializedState = new SerializedObject(renderState);
                    SerializedProperty renderers = serializedState.FindProperty(RENDER_STATE_RENDERERS_PROPERTY);
                    List<Object> listed = Read(renderers);
                    List<Object> kept = listed.Where(renderer => !UsesEngineGlow((Renderer)renderer)).ToList();
                    removed += listed.Count - kept.Count;
                    SetArray(renderers, kept);
                    serializedState.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return $"  {prefabPath}: {glows.Length} glows, {removed} removed from render state";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int VariantDepth(string prefabPath)
        {
            int depth = 0;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            while (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant)
            {
                prefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                depth++;
            }

            return depth;
        }

        private static bool IsEngineGlow(Material material) => AssetDatabase.GetAssetPath(material.shader) == SHADER_PATH;

        private static List<Object> Read(SerializedProperty array) =>
            Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).ToList();

        private static void SetArray<T>(SerializedProperty array, IReadOnlyList<T> values) where T : Object
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
