using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildISDIIReplacementArt
{
    const string MANIFEST = "Temp/ISDIIReplacement/ArtManifest.json";
    const string SHIP = "ISDII";
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
                string name = folder + "_" + (stem ?? (sourceShader=="alDefault.fx"?"Untextured":"Helper")).Replace(folder+"_", "") + "_" + sourceShader.Replace(".fx", "");
                string path = materialFolder + name + ".mat";
                bool additive = sourceShader.Contains("Additive") || sourceShader.Contains("MeshShield");
                Material material;
                if (File.Exists(path)) material=AssetDatabase.LoadAssetAtPath<Material>(path);
                else { material=new Material(Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit")); AssetDatabase.CreateAsset(material,path); }
                material.shader=Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit");
                if (stem != null)
                {
                    ConfigureTexture(textureFolder+stem+".png",false,false);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder+stem+(additive?"_Additive":"")+".png"));
                    if (!additive && sourceShader.Contains("Colorize"))
                    {
                        ConfigureTexture(textureFolder+stem+"_TeamMask.png",true,false);
                        material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder+stem+"_TeamMask.png"));
                        material.SetFloat("_TeamMaskStrength",0);
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
                    material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlendAlpha",1);material.SetFloat("_DstBlendAlpha",10); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
                    if(sourceShader=="MeshShield.fx" && stem.Contains("Engine_Glow"))ConfigureEngine(material);
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
                string name=folder+"_"+(stem??(sourceShader=="alDefault.fx"?"Untextured":"Helper")).Replace(folder+"_", "")+"_"+sourceShader.Replace(".fx", "");
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),(string)data["name"]),AssetDatabase.LoadAssetAtPath<Material>(materialFolder+name+".mat"));
            }
            modelImporter.globalScale=.02f;modelImporter.importAnimation=false;modelImporter.preserveHierarchy=true;modelImporter.SaveAndReimport();
            _models[entry.Name]=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        }
        var root=new GameObject(SHIP);
        try
        {
            var container=new GameObject("Geometry");container.transform.SetParent(root.transform,false);
            var hull=AddModel("ISDIIHull",container.transform,manifest);
            var audit=JObject.Parse(File.ReadAllText("Temp/ISDIIReplacement/SourceAudit.json"));
            foreach(var attachment in audit["attachments"])
                AddModel((string)attachment["variant"],Bone(hull,(string)attachment["bone"]),manifest);
            var bounds=BoundsOf(Points(root));float scale=180/bounds.size.z;
            container.transform.localScale=Vector3.one*scale;
            container.transform.localPosition=-bounds.center*scale;
            Directory.CreateDirectory("Assets/Prefabs/Models/Wrecks/Source");AssetDatabase.Refresh();
            var deathRoot=new GameObject("ISDIIShipView");
            try
            {
                var deathContainer=new GameObject("Geometry");deathContainer.transform.SetParent(deathRoot.transform,false);
                var death=AddModel("ISDIIDeath",deathContainer.transform,manifest);
                AddModel("ISDIIParts",Bone(death,"Decal_Attach_04"),manifest);
                deathContainer.transform.localScale=container.transform.localScale;
                deathContainer.transform.localPosition=container.transform.localPosition;
                StripHidden(deathRoot);
                PrefabUtility.SaveAsPrefabAsset(deathRoot,"Assets/Prefabs/Models/Wrecks/Source/ISDIIShipView.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(deathRoot);}
            StripHidden(root);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Models/Ships/ISDII.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();
        return "ISD II: own source hull + XML-resolved structure and all 13 turrets; length 180; no ISD I dependencies.";
    }
    public static string FixEngines()
    {
        foreach(string color in new[]{"Blue","White"})
        {
            string path="Assets/Art/Materials/Models/EmpireShips/ISDIIReplacement/ISDIIReplacement_ISDII_Engine_Glow_Fancy_"+color+"_MeshShield.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            ConfigureEngine(material);EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        return "Saved two-sided, emissive ISD II engine materials.";
    }
    static void ConfigureEngine(Material material)
    {
        // Source MeshShield engine surfaces include inward-facing exhaust caps.
        material.SetFloat("_Cull",(float)CullMode.Off);
        material.SetTexture("_EmissionMap",material.GetTexture("_BaseMap"));
        material.SetColor("_EmissionColor",new Color(2,2,2,1));
        // URP validation derives _EMISSION from the GI flags on import/save.
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.EnableKeyword("_EMISSION");
    }
    public static void StripHidden(GameObject root)
    {
        PrefabUtility.UnpackPrefabInstance(root.transform.GetChild(0).GetChild(0).gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r=>!r.enabled).ToArray())
        {
            if(renderer.transform.childCount==0)UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            else
            {
                var filter=renderer.gameObject.GetComponents<MeshFilter>().SingleOrDefault();
                if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);
                UnityEngine.Object.DestroyImmediate(renderer);
            }
        }
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
