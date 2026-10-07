using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class RepairMC80UnitySurfaces
{
    const string MODEL = "Assets/Art/Models/RebellionShips/MC80Independence/MC80Independence.fbx";
    const string MATERIALS = "Assets/Art/Materials/Models/RebellionShips/MC80Independence/";
    const string TEXTURES = "Assets/Art/Textures/Models/RebellionShips/MC80Independence/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC80Independence.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Surface repair requires Edit Mode.");
        AssetDatabase.Refresh();
        var report=JObject.Parse(File.ReadAllText("Temp/MC80IndependenceImport/SurfaceRepairReport.json"));
        var importer=(ModelImporter)AssetImporter.GetAtPath(MODEL);
        foreach(var definition in report["materials"])
        {
            string name=(string)definition["name"], sourceShader=(string)definition["shader"];
            bool additive=sourceShader.Contains("Additive");
            var material=new Material(Shader.Find(additive?"Universal Render Pipeline/Particles/Unlit":"EmpireAtWar/Ship Lit")){name=name};
            if(definition["base"].Type!=JTokenType.Null)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+(string)definition["base"]+".png");
                var textureImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                if(textureImporter.wrapMode!=TextureWrapMode.Repeat){textureImporter.wrapMode=TextureWrapMode.Repeat;textureImporter.SaveAndReimport();}
                material.SetTexture("_BaseMap",texture);
            }
            material.SetColor("_BaseColor",Color.white);
            if(additive)
            {
                material.SetFloat("_Surface",1);material.SetFloat("_Blend",2);
                material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            }
            else
            {
                material.SetFloat("_Smoothness",.25f);material.SetFloat("_Metallic",.1f);
                material.SetFloat("_TeamLiveryStrength",0);material.SetFloat("_TeamMaskStrength",0);
                material.SetFloat("_TeamRimStrength",sourceShader.Contains("Colorize")?.6f:0);
                if(definition["normal"].Type!=JTokenType.Null)material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+(string)definition["normal"]+".png"));
            }
            string path=MATERIALS+name+".mat";
            var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}
            else{EditorUtility.CopySerialized(material,saved);UnityEngine.Object.DestroyImmediate(material);EditorUtility.SetDirty(saved);}
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),saved);
        }
        importer.SaveAndReimport();
        var meshes=AssetDatabase.LoadAssetAtPath<GameObject>(MODEL).GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name,f=>f.sharedMesh);
        foreach(string path in new[]{VISUAL,PREVIEW})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var source=report["mesh_materials"].Single(r=>(string)r["name"]==renderer.name);
                    var slots=source["materials"].ToArray();
                    renderer.GetComponent<MeshFilter>().sharedMesh=meshes[renderer.name];
                    bool opaque=slots.All(s=>!((string)s["shader"]).Contains("Additive"));
                    renderer.sharedMaterials=slots.Select(s=>path==PREVIEW
                        ?AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat")
                        :AssetDatabase.LoadAssetAtPath<Material>(MATERIALS+(string)s["materialName"]+".mat")).ToArray();
                    renderer.enabled=!(bool)report["before"]["meshes"][renderer.name]["hidden"] && (path==VISUAL || opaque);
                }
                if(path==PREVIEW)Assign(Component(root,"UnitSpawnView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray());
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var gameplay=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        try
        {
            var opaque=gameplay.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            if(opaque.Length!=8)throw new InvalidOperationException("All eight original opaque meshes must be present.");
            Assign(Component(gameplay,"TeamColorView"),"meshRenderers",opaque);
            Assign(Component(gameplay,"Ship"),"explosionHullRenderers",opaque);
            PrefabUtility.SaveAsPrefabAsset(gameplay,GAMEPLAY);
        }
        finally{PrefabUtility.UnloadPrefabContents(gameplay);}
        AssetDatabase.SaveAssets();
        return "Original ALO materials restored: Hull four slots, meshes 1/3 opaque, eight opaque renderers bound to ownership/explosion/preview.";
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static void Assign(MonoBehaviour component,string name,UnityEngine.Object[] values)
    {
        var serialized=new SerializedObject(component);var array=serialized.FindProperty(name);array.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
