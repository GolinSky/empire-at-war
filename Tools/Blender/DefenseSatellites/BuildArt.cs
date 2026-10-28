using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildSatelliteArt
{
    private const string TASK = "Temp/DefenseSatellitesImport/";
    private const string MODELS = "Assets/Art/Models/DefendPlatforms/DefenseSatellites/";
    private const string TEXTURES = "Assets/Art/Textures/Models/DefendPlatforms/DefenseSatellites/";
    private const string MATERIALS = "Assets/Art/Materials/Models/DefendPlatforms/DefenseSatellites/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Satellite import requires Edit Mode.");
        AssetDatabase.Refresh();
        var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        foreach (string path in Directory.GetFiles(TEXTURES, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            bool normal = audit["textures"].Children<JProperty>().Any(t => (bool)t.Value["normal"] && path.EndsWith((string)t.Value["name"] + ".png"));
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !path.Contains("TeamMask");
            importer.maxTextureSize = 2048;
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
                    string path = MATERIALS + "DefenseSatellites_Slot" + materials.Count.ToString("D2") + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = shader.Contains("Additive")
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
                    if (!shader.Contains("Additive"))
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
        var results = new JArray();
        foreach (string type in new[] { "Base", "QL", "TL", "Ion", "Massdriver", "Missile" })
        {
            string name = type switch { "Base" => "SatelliteBase", "QL" => "QuadLaserSatellite", "TL" => "TurbolaserSatellite", "Massdriver" => "MassDriverSatellite", _ => type + "Satellite" };
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                AddGeometry(root.transform, "Turret_Space_Base", reports);
                if (type != "Base")
                {
                    var bones = (JArray)audit["models"]["Turret_Space_Base"]["bones"];
                    for (int i=0;i<6;i++)
                    {
                        var bone = bones.Single(b => ((string)b["name"]).Equals("turret_"+i.ToString("D2"), StringComparison.OrdinalIgnoreCase));
                        var m = Matrix4x4.identity;
                        for(int r=0;r<3;r++) for(int c=0;c<4;c++) m[r,c]=(float)bone["matrix"][r*4+c];
                        var pose=ToUnity(m);
                        var mount=new GameObject("Turret_"+i.ToString("D2")).transform;
                        mount.SetParent(root.transform,false);
                        mount.SetLocalPositionAndRotation(pose.GetColumn(3),pose.rotation);
                        AddGeometry(mount,"Turret_Space_"+type+"_"+i.ToString("D2"),reports);
                    }
                }
                // One common scale keeps the empty pad and all weapon variants aligned.
                foreach(Transform child in root.transform) { child.localPosition *= 5f; child.localScale *= 5f; }
                string path="Assets/Prefabs/Models/DefendStation/"+name+".prefab";
                PrefabUtility.SaveAsPrefabAsset(root,path);
                var bounds=BoundsOf(root);
                results.Add(new JObject { ["prefab"]=path,["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z) });
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK+"ArtReport.json",results.ToString());
        return results.ToString();
    }
    private static void AddGeometry(Transform parent,string name,JArray reports)
    {
        var raw=AssetDatabase.LoadAssetAtPath<GameObject>(MODELS+name+".fbx");
        var report=reports.Single(r=>(string)r["name"]==name);
        if(name!="Turret_Space_Base")
        {
            var muzzle=raw.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="MUZZLEA_00");
            var anchor=new GameObject("Muzzle").transform;
            anchor.SetParent(parent,false);
            anchor.SetLocalPositionAndRotation(muzzle.position,muzzle.rotation);
        }
        foreach(var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
        {
            if((bool)report["source"]["meshes"][renderer.name]["hidden"]) continue;
            var go=new GameObject(renderer.name);
            go.transform.SetParent(parent,false);
            go.transform.SetLocalPositionAndRotation(renderer.transform.position,renderer.transform.rotation);
            go.transform.localScale=renderer.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
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
