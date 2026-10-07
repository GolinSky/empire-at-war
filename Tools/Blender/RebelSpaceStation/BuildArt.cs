using System;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildRebelStationArt
{
    private const string TASK = "Temp/RebelStationImport/";
    private const string TEXTURES = "Assets/Art/Textures/Models/SpaceStations/RebelSpaceStation/";
    private const string MATERIALS = "Assets/Art/Materials/Models/SpaceStations/RebelSpaceStation/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station import requires Edit Mode.");
        AssetDatabase.Refresh();
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TEXTURES.TrimEnd('/') }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            bool normal = importer.assetPath.Contains("Bump");
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !importer.assetPath.Contains("TeamMask");
            if (normal) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
            importer.SaveAndReimport();
        }
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath((int)report["level"]));
            importer.globalScale = .02f;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            foreach (var remap in importer.GetExternalObjectMap().Keys.Where(key => key.name.StartsWith("RebelStationSlot")))
                importer.RemoveRemap(remap);
            foreach (var row in report["materials"])
            {
                string sourceName = (string)row["name"];
                string path = MATERIALS + "RebelSpaceStation_" + sourceName.Split('.')[0] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                string sourceShader = (string)row["shader"];
                if (material == null)
                {
                    material = sourceShader.Contains("Additive")
                        ? new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat"))
                        : new Material(Shader.Find(sourceShader.Contains("Bump") ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                var properties = row["properties"];
                foreach (var pair in new[] { new[] { "BaseTexture", "_BaseMap" }, new[] { "NormalTexture", "_BumpMap" } })
                {
                    string texture = (string)properties[pair[0]];
                    if (texture == null) continue;
                    string filename = (string)audit["textures"][texture.ToLowerInvariant()]["name"];
                    material.SetTexture(pair[1], AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + filename + ".png"));
                    if (pair[0] == "NormalTexture") material.EnableKeyword("_NORMALMAP");
                }
                if (sourceShader.Contains("Bump"))
                {
                    material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + "RebelSpaceStation_Hull_TeamMask.png"));
                    material.SetFloat("_TeamMaskStrength", 1);
                    material.SetFloat("_TeamLiveryStrength", 0);
                    material.SetFloat("_Metallic", .15f);
                    material.SetFloat("_Smoothness", .25f);
                }
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), material);
            }
            importer.SaveAndReimport();
        }

        var donor = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "RepublicSpaceStationView.prefab");
        var donorRenderers = new SerializedObject(donor.GetComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>()).FindProperty("explosionHullRenderers");
        Bounds donorBounds = ((Renderer)donorRenderers.GetArrayElementAtIndex(0).objectReferenceValue).bounds;
        for (int i = 1; i < donorRenderers.arraySize; i++) donorBounds.Encapsulate(((Renderer)donorRenderers.GetArrayElementAtIndex(i).objectReferenceValue).bounds);
        float targetWidth = Mathf.Max(donorBounds.size.x, donorBounds.size.z);
        float commonScale = 0;
        var saved = new JArray();
        foreach (var report in reports.Reverse())
        {
            int level = (int)report["level"];
            string name = (string)report["name"];
            var root = new GameObject(name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(level)), root.transform);
                foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mesh = report["source"]["meshes"][renderer.name];
                    if (mesh == null) throw new InvalidOperationException("Unknown source mesh: " + renderer.name);
                    renderer.enabled = !(bool)mesh["hidden"];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    if (renderer.name.EndsWith(" Shadow") && renderer.transform.childCount == 0)
                        UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                }
                Bounds bounds = BoundsOf(root);
                if (level == 5) commonScale = targetWidth / Mathf.Max(bounds.size.x, bounds.size.z);
                model.transform.localScale *= commonScale;
                model.transform.localPosition = -bounds.center * commonScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
                bounds = BoundsOf(root);
                var levelModel = root.AddComponent<StationLevelModel>();
                var serialized = new SerializedObject(levelModel);
                var hull = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
                Assign(serialized.FindProperty("<HullRenderers>k__BackingField"), hull);
                serialized.FindProperty("<HullBounds>k__BackingField").boundsValue = bounds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // Unity permits repeated transform names; restore the original ALO names after FBX transport.
                var sourceNames = (JObject)report["source_bone_names"];
                foreach (Transform bone in model.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.GetComponent<Renderer>() != null || sourceNames[bone.name] == null) continue;
                    bone.name = (string)sourceNames[bone.name];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(bone.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PREFABS + name + ".prefab");
                saved.Add(new JObject { ["level"] = level, ["prefab"] = PREFABS + name + ".prefab", ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z), ["commonScale"] = commonScale });
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "ArtReport.json", saved.ToString());
        return saved.ToString();
    }

    private static string ModelPath(int level) => "Assets/Art/Models/SpaceStations/RebelSpaceStation/Level" + level + "/RebelSpaceStationLevel" + level + ".fbx";
    private static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
            .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(r.transform.TransformPoint)).ToArray();
        Bounds bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points.Skip(1)) bounds.Encapsulate(point);
        return bounds;
    }
    private static void Assign(SerializedProperty property, UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
}
