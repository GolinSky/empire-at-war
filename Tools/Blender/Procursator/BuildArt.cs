using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class BuildProcursatorArt
{
    const string NAME = "Procursator";
    const string MODELS = "Assets/Art/Models/EmpireShips/Procursator/";
    const string TEXTURES = "Assets/Art/Textures/Models/EmpireShips/Procursator/";
    const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/Procursator/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Procursator.prefab";
    const float LENGTH = 126.5464f;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship construction requires Edit Mode.");
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]{TEXTURES.TrimEnd('/')}))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !(importer.assetPath.Contains("Normal") || importer.assetPath.Contains("_BC.png")) && !importer.assetPath.Contains("TeamMask");
            if ((importer.assetPath.Contains("Normal") || importer.assetPath.Contains("_BC.png"))) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
            importer.SaveAndReimport();
        }
        var report = JObject.Parse(File.ReadAllText("Temp/ProcursatorImport/ConversionReport.json"));
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"], path = MATERIALS+name+".mat";
            bool additive = ((string)source["shader"]).Contains("Additive");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit")); AssetDatabase.CreateAsset(material,path); }
            string texture = (string)source["base"], normal = (string)source["normal"];
            if (texture != null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+".png"));
            if (additive)
            {
                material.SetFloat("_Surface",1); material.SetFloat("_Blend",2); material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",1); material.SetFloat("_ZWrite",0); material.SetFloat("_Cull",0); material.SetFloat("_SrcBlendAlpha",1); material.SetFloat("_DstBlendAlpha",10);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000;
            }
            else
            {
                material.SetFloat("_Smoothness",.25f); material.SetFloat("_Metallic",.1f);
                if (normal != null) { material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+normal+".png")); material.EnableKeyword("_NORMALMAP"); }
                // Source Colorize shaders use inverse-alpha paint masks.
                bool hullPaint = texture != null && texture.Contains("Cruiser_AllMC30C_CL") && !texture.EndsWith("_BC");
                material.SetFloat("_TeamMaskStrength",hullPaint?1:0); if(hullPaint) material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+"_TeamMask.png")); material.SetFloat("_TeamLiveryHue",0); material.SetFloat("_TeamLiveryHueRange",.07f);
                material.SetFloat("_TeamLiveryMinSaturation",.3f); material.SetFloat("_TeamLiveryStrength",0); material.SetFloat("_TeamRimStrength",0);
            }
            EditorUtility.SetDirty(material);
        }
        foreach (var record in ((JObject)report["models"]).Properties())
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(MODELS+record.Name+".fbx");
            importer.globalScale=.02f; importer.preserveHierarchy=true; importer.importAnimation=false; importer.isReadable=true;
            importer.weldVertices=false; importer.meshCompression=ModelImporterMeshCompression.Off;
            foreach (var source in report["materials"])
            {
                string name=(string)source["name"];
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),AssetDatabase.LoadAssetAtPath<Material>(MATERIALS+name+".mat"));
            }
            importer.SaveAndReimport();
        }
        var root=new GameObject(NAME);
        try
        {
            var hull=Instantiate(NAME,root.transform,report);
            var bones=hull.GetComponentsInChildren<Transform>(true);
            var audit=JObject.Parse(File.ReadAllText("Temp/ProcursatorImport/SourceAudit.json"));
            foreach(var attachment in audit["attachments"])
            {
                var bone=bones.Single(t=>t.name==(string)attachment["bone"] && t.GetComponent<Renderer>()==null);
                var part=Instantiate((string)attachment["variant"],bone,report);
                part.transform.localPosition=Vector3.zero;
                part.transform.localRotation=Quaternion.Euler(90,0,0);
                part.transform.localScale=Vector3.one*.01f;
            }
            var bounds=BoundsOf(root); float scale=LENGTH/bounds.size.z;
            hull.transform.localScale*=scale; hull.transform.localPosition=-bounds.center*scale;
            bounds=BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root,VISUAL); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/ProcursatorImport/ArtReport.json",new JObject{["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["scale"]=scale,["visibleMeshes"]=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)}.ToString());
            return "Saved Procursator hull, bounds "+bounds.size;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static GameObject Instantiate(string name,Transform parent,JObject report)
    {
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS+name+".fbx"),parent);
        PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        model.name=name;
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            string sourceName=renderer.name.Split('.')[0]; renderer.name=sourceName;
            bool hidden=(bool)report["models"][name]["before"]["meshes"][sourceName]["hidden"];
            if(hidden) { UnityEngine.Object.DestroyImmediate(renderer.gameObject); continue; }
            if(!(renderer is SkinnedMeshRenderer)) continue;
            var skinned=(SkinnedMeshRenderer)renderer; var mesh=new Mesh(); skinned.BakeMesh(mesh,false);
            string path=MODELS+name+"_"+sourceName+"Mesh.asset"; mesh.name=name+"_"+sourceName+"Mesh";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) AssetDatabase.CreateAsset(mesh,path); else { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; EditorUtility.SetDirty(mesh); }
            var owner=renderer.gameObject; var materials=renderer.sharedMaterials; UnityEngine.Object.DestroyImmediate(renderer);
            owner.AddComponent<MeshFilter>().sharedMesh=mesh; owner.AddComponent<MeshRenderer>().sharedMaterials=materials;
        }
        return model;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds=new Bounds(points[0],Vector3.zero); foreach(var point in points)bounds.Encapsulate(point); return bounds;
    }
}
