using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class BuildTitanArt
{
    const string NAME = "Titan";
    const string MODELS = "Assets/Art/Models/EmpireShips/Titan/";
    const string TEXTURES = "Assets/Art/Textures/Models/EmpireShips/Titan/";
    const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/Titan/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Titan.prefab";
    

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship construction requires Edit Mode.");
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]{TEXTURES.TrimEnd('/')}))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !(importer.assetPath.EndsWith("_B.png")) && !importer.assetPath.Contains("TeamMask");
            if ((importer.assetPath.EndsWith("_B.png"))) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
            importer.SaveAndReimport();
        }
        var report = JObject.Parse(File.ReadAllText("Temp/TitanImport/ConversionReport.json"));
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"], path = MATERIALS+name+".mat";
            bool additive = (((string)source["shader"]).Contains("Additive") || ((string)source["shader"]).Contains("Shield"));
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit")); AssetDatabase.CreateAsset(material,path); }
            string texture = (string)source["base"], normal = (string)source["normal"];
            if (texture != null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+(additive?"_Additive":"")+".png"));
            if (additive)
            {
                material.SetFloat("_Surface",1); material.SetFloat("_Blend",2); material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",1); material.SetFloat("_ZWrite",0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000;
            }
            else
            {
                material.SetFloat("_Smoothness",.25f); material.SetFloat("_Metallic",.1f);
                if (normal != null) { material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+normal+".png")); material.EnableKeyword("_NORMALMAP"); }
                // Original alpha masks are empty. Recolor authored red hull markings instead.
                bool hullPaint = texture != null && texture.Contains("Empire_Titan_Star_Destroyer");
                material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+"_TeamMask.png")); material.SetFloat("_TeamMaskStrength",0); material.SetFloat("_TeamLiveryHue",0); material.SetFloat("_TeamLiveryHueRange",.07f);
                material.SetFloat("_TeamLiveryMinSaturation",.3f); material.SetFloat("_TeamLiveryStrength",0); material.SetFloat("_TeamRimStrength",.6f);
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
            var audit=JObject.Parse(File.ReadAllText("Temp/TitanImport/SourceAudit.json"));
            foreach(var attachment in audit["attachments"])
            {
                string bone=(string)attachment["bone"];
                var socket=bones.Single(t=>t.name.Equals(bone,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
                var part=Instantiate((string)attachment["variant"],socket,report);
                part.transform.localPosition=Vector3.zero;
                part.transform.localRotation=Quaternion.Euler(90,0,0);
                part.transform.localScale=Vector3.one*.01f;
            }
            var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/VenatorShipView.prefab");
            var referencePoints=reference.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>reference.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            float length=(referencePoints.Max(v=>v.z)-referencePoints.Min(v=>v.z))*4500f/1137f;
            var bounds=BoundsOf(root); float scale=length/bounds.size.z;
            hull.transform.localScale*=scale; hull.transform.localPosition=-bounds.center*scale;
            bounds=BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root,VISUAL); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/TitanImport/ArtReport.json",new JObject{["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["scale"]=scale,["visibleMeshes"]=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)}.ToString());
            return "Saved Titan hull and 106 source turret attachments, bounds "+bounds.size;
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
            string sourceName=renderer.name;
            bool hidden=(bool)report["models"][name]["before"]["meshes"][sourceName]["hidden"];
            if(sourceName=="Stealth")renderer.enabled=false;
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
