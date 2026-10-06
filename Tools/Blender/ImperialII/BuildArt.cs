using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildImperialIIArt
{
    const string MANIFEST = "Temp/ImperialIIImport/ArtManifest.json";
    const string SHIP = "ImperialII";
    static readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();
    public static string Main()
    {
        AssetDatabase.Refresh();
        var manifest = JObject.Parse(File.ReadAllText(MANIFEST));
        foreach (var entry in manifest.Properties())
        {
            var report = entry.Value;
            string folder = (string)report["folder"], modelPath = (string)report["model_path"];
            string textureFolder = "Assets/Art/Textures/Models/EmpireShips/" + folder + "/";
            string materialFolder = "Assets/Art/Materials/Models/EmpireShips/" + folder + "/";
            Directory.CreateDirectory(materialFolder); AssetDatabase.Refresh();
            foreach (var data in report["materials"])
            {
                string sourceShader=(string)data["shader"], stem=(string)data["base"], normal=(string)data["normal"];
                string name = folder + "_" + (stem ?? "Helper").Replace(folder+"_", "") + "_" + sourceShader.Replace(".fx", "");
                string path = materialFolder + name + ".mat";
                bool additive = sourceShader.Contains("Additive");
                Material material;
                if (File.Exists(path)) material=AssetDatabase.LoadAssetAtPath<Material>(path);
                else { material=new Material(Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit")); AssetDatabase.CreateAsset(material,path); }
                if (stem != null)
                {
                    ConfigureTexture(textureFolder+stem+".png",false,false);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder+stem+".png"));
                    if (!additive && sourceShader.Contains("Colorize"))
                    {
                        ConfigureTexture(textureFolder+stem+"_TeamMask.png",true,false);
                        material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder+stem+"_TeamMask.png"));
                        material.SetFloat("_TeamMaskStrength",.2f);
                    }
                }
                if (normal != null)
                {
                    ConfigureTexture(textureFolder+normal+".png",true,true);
                    material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder+normal+".png"));
                    material.EnableKeyword("_NORMALMAP");
                }
                if (additive)
                {
                    material.SetFloat("_Surface",1);material.SetFloat("_Blend",2);material.SetFloat("_SrcBlend",(float)BlendMode.One);material.SetFloat("_DstBlend",(float)BlendMode.One);
                    material.SetFloat("_ZWrite",0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
                }
                else { material.SetFloat("_Smoothness",.25f);material.SetFloat("_Metallic",.1f);material.SetFloat("_TeamLiveryStrength",0); }
                EditorUtility.SetDirty(material);
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),(string)data["name"]),material);
            }
            var modelImporter=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            // Texture reimports can reload importers. Apply the entire remap together.
            foreach(var data in report["materials"])
            {
                string sourceShader=(string)data["shader"],stem=(string)data["base"];
                string name=folder+"_"+(stem??"Helper").Replace(folder+"_", "")+"_"+sourceShader.Replace(".fx", "");
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),(string)data["name"]),AssetDatabase.LoadAssetAtPath<Material>(materialFolder+name+".mat"));
            }
            modelImporter.globalScale=.02f;modelImporter.importAnimation=false;modelImporter.preserveHierarchy=true;modelImporter.SaveAndReimport();
            _models[entry.Name]=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        }
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialIAdvanced.prefab"));
        try
        {
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            root.name=SHIP;
            foreach(var part in root.GetComponentsInChildren<Transform>(true).Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,"^ImperialIAdvanced(Parts|TLD[0-9]{2}|ICD[0-9]{2})$")).ToArray())
                UnityEngine.Object.DestroyImmediate(part.gameObject);
            var hull=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="ImperialIAdvanced").gameObject;
            AddModel(SHIP+"Parts",Bone(hull,"Decal_Attach_04"),manifest);
            for(int i=1;i<=8;i++)AddModel(SHIP+"TLO"+i.ToString("00"),Bone(hull,"TA_ISD2_TLO_"+i.ToString("00")),manifest);
            for(int i=1;i<=2;i++)AddModel(SHIP+"ICQ"+i.ToString("00"),Bone(hull,"TA_ISD_ICQ_"+i.ToString("00")),manifest);
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))transform.name=transform.name.Replace("ImperialIAdvanced","ImperialII");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Models/Ships/ImperialII.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();
        return "Imperial II: verified common hull, own hull parts, eight rapid dual turrets, two long-range turbo-ion turrets and three common triple turrets.";
    }
    static GameObject AddModel(string variant,Transform parent,JObject manifest)
    {
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(_models[variant],parent);obj.name=variant;
        if(parent.name!="Geometry")
        {
            // The exported armature has a 100x scale and a -90 degree X basis.
            // Cancel that transport basis before nesting another exported model.
            obj.transform.localScale=Vector3.one*.01f;
            obj.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        var report=manifest[variant];
        foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            renderer.enabled=!(bool)report["after"]["meshes"][renderer.name]["hidden"];
        foreach(var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.enabled).ToArray())
        {
            var mesh=new Mesh();skin.BakeMesh(mesh,true);mesh.name=variant+"_"+skin.name;
            string path="Assets/Art/Models/EmpireShips/"+(string)report["folder"]+"/"+mesh.name+"_Static.asset";
            if(File.Exists(path))
            {
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);saved.Clear();saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.uv=mesh.uv;saved.triangles=mesh.triangles;saved.bounds=mesh.bounds;
                UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);
            }
            else AssetDatabase.CreateAsset(mesh,path);
            var visual=new GameObject(skin.name+"_Static");visual.transform.SetParent(skin.transform,false);
            var filter=visual.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;
            skin.enabled=false;
        }
        return obj;
    }
    public static Transform Bone(GameObject model,string name)=>model.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(name,StringComparison.OrdinalIgnoreCase));
    public static Vector3[] Points(GameObject root)=>root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.GetComponents<MeshRenderer>().Single().enabled).SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();
    public static Bounds BoundsOf(Vector3[] points){var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;}
    static void ConfigureTexture(string path,bool linear,bool normal)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=!linear;importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        if(normal)importer.flipGreenChannel=true;
        importer.SaveAndReimport();
    }
}
