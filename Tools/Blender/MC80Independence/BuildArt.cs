using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class BuildMC80IndependenceArt
{
    const string MODEL = "Assets/Art/Models/RebellionShips/MC80Independence/MC80Independence.fbx";
    const string TEXTURES = "Assets/Art/Textures/Models/RebellionShips/MC80Independence/";
    const string MATERIALS = "Assets/Art/Materials/Models/RebellionShips/MC80Independence/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC80Independence.prefab";
    const float LENGTH = 220;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Independence construction requires Edit Mode.");
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]{TEXTURES.TrimEnd('/')}))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !importer.assetPath.EndsWith("_B.png") && !importer.assetPath.Contains("TeamMask");
            if (importer.assetPath.EndsWith("_B.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.flipGreenChannel = true;
            }
            importer.SaveAndReimport();
        }
        var report = JObject.Parse(System.IO.File.ReadAllText("Temp/MC80IndependenceImport/ConversionReport.json"));
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        modelImporter.globalScale = .02f;
        modelImporter.preserveHierarchy = true;
        modelImporter.importAnimation = false;
        modelImporter.isReadable = true;
        modelImporter.weldVertices = false;
        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"];
            bool additive = ((string)source["shader"]).Contains("Additive");
            var material = new Material(Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit"));
            string texture = (string)source["base"];
            if (texture != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+".png"));
            if (additive)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 2);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Smoothness", .25f);
                material.SetFloat("_Metallic", .1f);
                material.SetFloat("_TeamLiveryStrength", 0);
                string normal = (string)source["normal"];
                if (normal != null) { material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+normal+".png")); material.EnableKeyword("_NORMALMAP"); }
                material.SetFloat("_TeamMaskStrength", 0);
                material.SetFloat("_TeamRimStrength", ((string)source["shader"]).Contains("Colorize") ? .6f : 0);

            }
            AssetDatabase.CreateAsset(material, MATERIALS+name+".mat");
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name), material);
        }
        modelImporter.SaveAndReimport();
        var root = new GameObject("MC80Independence");
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL),root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = !(bool)report["before"]["meshes"][renderer.name]["hidden"];
                if (!(renderer is SkinnedMeshRenderer)) continue;
                var skinned = (SkinnedMeshRenderer)renderer;
                var mesh = new Mesh();
                skinned.BakeMesh(mesh, false);
                mesh.name = renderer.name+"Mesh";
                AssetDatabase.CreateAsset(mesh, System.IO.Path.GetDirectoryName(MODEL).Replace('\\','/')+"/"+mesh.name+".asset");
                var owner = renderer.gameObject;
                var materials = renderer.sharedMaterials;
                bool enabled = renderer.enabled;
                UnityEngine.Object.DestroyImmediate(renderer);
                owner.AddComponent<MeshFilter>().sharedMesh = mesh;
                var replacement = owner.AddComponent<MeshRenderer>();
                replacement.sharedMaterials = materials;
                replacement.enabled = enabled;
            }
            var bounds = BoundsOf(root);
            float scale = LENGTH / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root,VISUAL);
            AssetDatabase.SaveAssets();
            System.IO.File.WriteAllText("Temp/MC80IndependenceImport/ArtReport.json", new JObject{["boundsCenter"]=new JArray(bounds.center.x,bounds.center.y,bounds.center.z),["boundsSize"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["scale"]=scale,["visibleMeshes"]=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)}.ToString());
            return "MC80Independence visual saved: "+bounds.size+"; scale "+scale;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    public static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0],Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }
}
