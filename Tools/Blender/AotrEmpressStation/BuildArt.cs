using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildEmpressArt
{
    private const string TASK = "Temp/AotrEmpressStationImport/";
    private const string MODELS = "Assets/Art/Models/SpaceStations/AotrEmpressStation/";
    private const string TEXTURES = "Assets/Art/Textures/Models/SpaceStations/AotrEmpressStation/";
    private const string MATERIALS = "Assets/Art/Materials/Models/SpaceStations/AotrEmpressStation/";
    private const string PREFAB = "Assets/Prefabs/Models/Stations/AotrEmpressStation.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Empress import requires Edit Mode.");
        AssetDatabase.Refresh();
        var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        foreach (string path in Directory.GetFiles(TEXTURES, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            bool normal = audit["textures"].Children<JProperty>().Any(t => (bool)t.Value["normal"] && path.EndsWith((string)t.Value["name"] + ".png"));
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !path.Contains("TeamMask");
            importer.maxTextureSize = 8192;
            if (normal) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
            importer.SaveAndReimport();
        }
        var materials = new Dictionary<string, Material>();
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            var importer = (ModelImporter)AssetImporter.GetAtPath(MODELS + name + ".fbx");
            importer.globalScale = .02f;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            foreach (var row in report["materials"])
            {
                string shader = (string)row["shader"];
                string signature = shader + row["properties"].ToString(Newtonsoft.Json.Formatting.None);
                if (!materials.TryGetValue(signature, out var material))
                {
                    string path = MATERIALS + "AotrEmpressStation_Slot" + materials.Count.ToString("D2") + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = shader.Contains("Additive") || shader.Contains("Shield")
                            ? new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat"))
                            : new Material(Shader.Find("EmpireAtWar/Ship Lit"));
                        AssetDatabase.CreateAsset(material, path);
                    }
                    foreach (var pair in new[] { new[] { "BaseTexture", "_BaseMap" }, new[] { "NormalTexture", "_BumpMap" } })
                    {
                        string reference = (string)row["properties"][pair[0]];
                        if (reference == null) continue;
                        string texture = (string)audit["textures"][reference.ToLowerInvariant()]["name"];
                        material.SetTexture(pair[1], AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + texture + ".png"));
                        if (pair[0] == "NormalTexture") material.EnableKeyword("_NORMALMAP");
                        if (pair[0] == "BaseTexture" && shader.Contains("Colorize"))
                            material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + texture + "_TeamMask.png"));
                    }
                    var tint = row["properties"][shader.Contains("Additive") || shader.Contains("Shield") ? "Color" : "Diffuse"];
                    if (tint != null) material.SetColor("_BaseColor",new Color((float)tint[0],(float)tint[1],(float)tint[2],1));
                    if (!shader.Contains("Additive") && !shader.Contains("Shield"))
                    {
                        material.SetFloat("_TeamMaskStrength", shader.Contains("Colorize") ? 1 : 0);
                        material.SetFloat("_TeamLiveryStrength", 0);
                        material.SetFloat("_TeamRimStrength", 0);
                        material.SetFloat("_Metallic", .15f);
                        material.SetFloat("_Smoothness", .25f);
                    }
                    EditorUtility.SetDirty(material);
                    materials.Add(signature, material);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)row["name"]), material);
            }
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("AotrEmpressStation");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + "AotrEmpressStation.fbx"), root.transform);
            var hullReport = reports.Single(r => (string)r["name"] == "AotrEmpressStation");
            foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = !(bool)hullReport["source"]["meshes"][renderer.name]["hidden"];
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            var bones = (JArray)audit["models"]["AotrEmpressStation"]["bones"];
            var matrices = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                var matrix = Matrix4x4.identity;
                for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) matrix[r,c] = (float)bones[i]["matrix"][r*4+c];
                long parent = (long)bones[i]["parent_index"];
                matrices[i] = parent == uint.MaxValue ? matrix : matrices[parent] * matrix;
            }
            foreach (var hp in audit["hardpoints"].Where(h => h["artModel"] != null))
            {
                string name = (string)hp["artModel"];
                string boneName = (string)hp["Attachment_Bone"];
                int index = Enumerable.Range(0,bones.Count).Single(i => ((string)bones[i]["name"]).Equals(boneName,StringComparison.OrdinalIgnoreCase));
                var anchor = model.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(boneName,StringComparison.OrdinalIgnoreCase));
                var pose = ToUnity(matrices[index]);
                var art = new GameObject((string)hp["name"] + "_Art").transform;
                art.SetParent(model.transform,false);
                art.SetLocalPositionAndRotation(pose.GetColumn(3),pose.rotation);
                art.localScale = pose.lossyScale;
                art.SetParent(anchor,true);
                var raw = AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + name + ".fbx");
                var report = reports.Single(r => (string)r["name"] == name);
                foreach (var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if ((bool)report["source"]["meshes"][renderer.name]["hidden"]) continue;
                    var geometry = new GameObject(renderer.name);
                    geometry.transform.SetParent(art,false);
                    geometry.transform.SetLocalPositionAndRotation(renderer.transform.position,renderer.transform.rotation);
                    geometry.transform.localScale = renderer.transform.lossyScale;
                    geometry.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    geometry.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
            }
            var bounds = BoundsOf(root);
            float scale = 300 / Mathf.Max(bounds.size.x,bounds.size.z);
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true).Where(r => !r.enabled).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                UnityEngine.Object.DestroyImmediate(renderer);
            }
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(bone);
                if (original == null || original.GetComponent<Renderer>() != null || hullReport["source_bone_names"][original.name] == null) continue;
                bone.name = (string)hullReport["source_bone_names"][original.name];
                PrefabUtility.RecordPrefabInstancePropertyModifications(bone.gameObject);
            }
            PrefabUtility.SaveAsPrefabAsset(root,PREFAB);
            AssetDatabase.SaveAssets();
            bounds = BoundsOf(root);
            File.WriteAllText(TASK + "ArtReport.json",new JObject { ["prefab"]=PREFAB,["materials"]=materials.Count,["artPieces"]=24,["nestedScale"]=scale,
                ["bounds"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z) }.ToString());
            return "Saved separate visual prefab with shared FBX/material references and 24 XML attachments.";
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    private static Matrix4x4 ToUnity(Matrix4x4 source)
    {
        var basis = new Matrix4x4(new Vector4(-1,0,0,0),new Vector4(0,0,-1,0),new Vector4(0,1,0,0),new Vector4(0,0,0,1));
        var result = basis * source * basis.inverse;
        Vector4 p = result.GetColumn(3);
        result.SetColumn(3,new Vector4(p.x*.02f,p.y*.02f,p.z*.02f,1));
        return result;
    }
    public static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(r.transform.TransformPoint)).ToArray();
        var bounds = new Bounds(points[0],Vector3.zero);
        foreach (var point in points.Skip(1)) bounds.Encapsulate(point);
        return bounds;
    }
}
