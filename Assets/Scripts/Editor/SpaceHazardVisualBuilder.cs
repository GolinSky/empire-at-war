using System.IO;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor
{
    public static class SpaceHazardVisualBuilder
    {
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/Vfx";
        private const string PREFAB_FOLDER = "Assets/Prefabs/Vfx/SpaceHazards";
        private const string MESH_PATH = "Assets/Art/Models/Other/SpaceHazards/HazardSheet.asset";
        private const int SHEET_COUNT = 5;

        [MenuItem("Tools/Empire At War/Build Space Hazard Visuals")]
        public static void Build()
        {
            Directory.CreateDirectory(PREFAB_FOLDER);
            Directory.CreateDirectory(Path.GetDirectoryName(MESH_PATH));
            Directory.CreateDirectory(MATERIAL_FOLDER);
            AssetDatabase.Refresh();
            Material nebula = CloudMaterial("NebulaHazard", false);
            Material ion = CloudMaterial("IonStormHazard", true);
            Material arc = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_FOLDER + "/SpaceHazardArc.mat");
            if (arc == null)
            {
                arc = new Material(Shader.Find("EmpireAtWar/SpaceHazardArc"));
                AssetDatabase.CreateAsset(arc, MATERIAL_FOLDER + "/SpaceHazardArc.mat");
            }
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
            if (mesh == null)
            {
                mesh = new Mesh { name = "HazardSheet" };
                mesh.vertices = new[] { new Vector3(-1,0,-1), new Vector3(-1,0,1), new Vector3(1,0,1), new Vector3(1,0,-1) };
                mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                mesh.triangles = new[] { 0,1,2,0,2,3 };
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, MESH_PATH);
            }
            HazardCloudView cloud = CreateCloud(mesh, nebula, arc);
            HazardEntryCue entry = CreateEntry(arc);
            foreach (string guid in AssetDatabase.FindAssets("t:SpaceHazardData", new[] { "Assets/Settings" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<SpaceHazardData>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(data);
                serialized.FindProperty("<VisualMaterial>k__BackingField").objectReferenceValue = data.HazardType == HazardType.IonStorm ? ion : nebula;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
            const string sceneContext = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(sceneContext);
            try
            {
                foreach (MapLayoutView view in root.GetComponentsInChildren<MapLayoutView>(true))
                {
                    var serialized = new SerializedObject(view);
                    serialized.FindProperty("hazardPrefab").objectReferenceValue = cloud;
                    
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var effects = root.GetComponentInChildren<HazardEffectsView>(true);
                if (effects == null) effects = root.AddComponent<HazardEffectsView>();
                var effectsSerialized = new SerializedObject(effects);
                effectsSerialized.FindProperty("hazardEntryPrefab").objectReferenceValue = entry;
                effectsSerialized.ApplyModifiedPropertiesWithoutUndo();
                var scope = root.GetComponent<EmpireAtWar.Services.VContainer.SkirmishScope>();
                var scopeSerialized = new SerializedObject(scope);
                scopeSerialized.FindProperty("hazardEffectsView").objectReferenceValue = effects;
                scopeSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, sceneContext);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            SpaceHazardUiBuilder.Build();
            DecorAsteroidVisualBuilder.Build();
            AssetDatabase.SaveAssets();
        }

        private static Material CloudMaterial(string name, bool ion)
        {
            string path = MATERIAL_FOLDER + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("EmpireAtWar/SpaceHazard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", ion ? new Color(0.16f,0.36f,0.85f,0.38f) : new Color(0.58f,0.26f,0.8f,0.36f));
            material.SetColor("_AccentColor", ion ? new Color(0.45f,0.9f,1f) : new Color(0.95f,0.45f,0.75f));
            material.SetFloat("_Ion", ion ? 1f : 0f);
            material.SetFloat("_Speed", ion ? 0.08f : 0.018f);
            material.SetFloat("_Feather", ion ? 20f : 28f);
            material.SetFloat("_WispReach", ion ? 70f : 100f);
            material.SetFloat("_Stretch", ion ? 0.55f : 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static HazardCloudView CreateCloud(Mesh mesh, Material material, Material arcMaterial)
        {
            var root = new GameObject("SpaceHazard");
            try
            {
                var view = root.AddComponent<HazardCloudView>();
                var serialized = new SerializedObject(view);
                SerializedProperty layers = serialized.FindProperty("layers");
                layers.arraySize = SHEET_COUNT;
                SerializedProperty filters = serialized.FindProperty("filters");
                filters.arraySize = SHEET_COUNT;
                for (int i = 0; i < SHEET_COUNT; i++)
                {
                    var layer = new GameObject("CloudLayer" + i);
                    layer.transform.SetParent(root.transform, false);
                    MeshFilter filter = layer.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    filters.GetArrayElementAtIndex(i).objectReferenceValue = filter;
                    MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    layers.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
                }
                var arcObject = new GameObject("Arc");
                arcObject.transform.SetParent(root.transform, false);
                serialized.FindProperty("arc").objectReferenceValue = AddLine(arcObject, arcMaterial, 2f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                string path = PREFAB_FOLDER + "/SpaceHazard.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return AssetDatabase.LoadAssetAtPath<HazardCloudView>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static HazardEntryCue CreateEntry(Material material)
        {
            var root = new GameObject("HazardEntryCue");
            try
            {
                var cue = root.AddComponent<HazardEntryCue>();
                var serialized = new SerializedObject(cue);
                serialized.FindProperty("line").objectReferenceValue = AddLine(root, material, 2f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                string path = PREFAB_FOLDER + "/HazardEntryCue.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return AssetDatabase.LoadAssetAtPath<HazardEntryCue>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static LineRenderer AddLine(GameObject target, Material material, float width)
        {
            LineRenderer line = target.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.startColor = new Color(0.65f, 0.9f, 1f);
            line.endColor = line.startColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            return line;
        }
    }
}
