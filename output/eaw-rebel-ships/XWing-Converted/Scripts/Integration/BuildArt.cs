using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildXWingArt
{
    const string MODEL="Assets/Art/Models/RebellionShips/XWing/XWing.fbx";
    const string TEXTURES="Assets/Art/Textures/Models/RebellionShips/XWing/";
    const string MATERIALS="Assets/Art/Materials/Models/RebellionShips/XWing/";
    const string VISUAL="Assets/Prefabs/Models/Squadrons/XWing.prefab";
    public static string Main()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{TEXTURES.TrimEnd('/')}))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=false;
            importer.sRGBTexture=importer.assetPath.Contains("Albedo");
            importer.SaveAndReimport();
        }
        var hull=new Material(Shader.Find("EmpireAtWar/Ship Lit"));
        hull.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+"XWing_Hull_Albedo.png"));
        hull.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+"XWing_Hull_TeamMask.png"));
        hull.SetFloat("_TeamMaskStrength",1);
        hull.SetFloat("_TeamLiveryStrength",0);
        hull.SetFloat("_Smoothness",.25f);
        hull.SetFloat("_Metallic",.1f);
        AssetDatabase.CreateAsset(hull,MATERIALS+"XWing_Hull.mat");
        var collision=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        AssetDatabase.CreateAsset(collision,MATERIALS+"XWing_Collision.mat");
        var flash=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        flash.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+"XWing_MuzzleFlash_Albedo.png"));
        AssetDatabase.CreateAsset(flash,MATERIALS+"XWing_MuzzleFlashHelper.mat");
        var modelImporter=(ModelImporter)AssetImporter.GetAtPath(MODEL);
        modelImporter.globalScale=.02f;
        modelImporter.importAnimation=true;
        modelImporter.animationType=ModelImporterAnimationType.Legacy;
        modelImporter.animationWrapMode=WrapMode.ClampForever;
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"rv_xwing Material"),hull);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"rv_xwing Material1"),hull);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"COLLISION"),collision);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"W_Laser_Small Material"),flash);
        modelImporter.SaveAndReimport();
        var clips=modelImporter.defaultClipAnimations;
        foreach(var clip in clips) { clip.name=clip.takeName.EndsWith("Undeploy")?"OpenSFoils":"CloseSFoils";clip.wrapMode=WrapMode.ClampForever; }
        modelImporter.clipAnimations=clips;
        modelImporter.SaveAndReimport();
        var visual=new GameObject("XWing");
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL),visual.transform);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.enabled=renderer.name=="X_Wing_LOD1";
            var animation=model.GetComponents<Animation>().Single();
            animation.playAutomatically=false;
            var open=AssetDatabase.LoadAllAssetsAtPath(MODEL).OfType<AnimationClip>().Single(c=>c.name=="OpenSFoils");
            open.SampleAnimation(model,open.length);
            var mesh=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.enabled);
            var baked=new Mesh();mesh.BakeMesh(baked);
            var points=baked.vertices.Select(v=>mesh.transform.TransformPoint(v)).ToArray();
            var min=new Vector3(points.Min(p=>p.x),points.Min(p=>p.y),points.Min(p=>p.z));
            var max=new Vector3(points.Max(p=>p.x),points.Max(p=>p.y),points.Max(p=>p.z));
            float scale=4f/(max.z-min.z);
            model.transform.localScale*=scale;
            model.transform.localPosition=-(min+max)*.5f*scale;
            mesh.updateWhenOffscreen=true;
            PrefabUtility.SaveAsPrefabAsset(visual,VISUAL);
            UnityEngine.Object.DestroyImmediate(baked);
            AssetDatabase.SaveAssets();
            return "Saved animated XWing visual, source hull/team mask, all four material remaps. Scale "+scale+"; clips "+string.Join(",",clips.Select(c=>c.name));
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }
}
