using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildVictoryIIAdvancedArt
{
    const string REPORT = "Temp/VictoryIIAdvancedImport/ConversionReport.json";
    public static string Main()
    {
        var reports = JArray.Parse(File.ReadAllText(REPORT));
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            string modelPath = "Assets/Art/Models/EmpireShips/"+name+"/"+name+".fbx";
            string textureFolder = "Assets/Art/Textures/Models/EmpireShips/"+name;
            string materialFolder = "Assets/Art/Materials/Models/EmpireShips/"+name;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{textureFolder}))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = false;
                importer.sRGBTexture = !importer.assetPath.Contains("Normal") && !importer.assetPath.Contains("TeamMask");
                if (importer.assetPath.Contains("Normal")) { importer.textureType=TextureImporterType.NormalMap; importer.flipGreenChannel=true; }
                importer.SaveAndReimport();
            }
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            modelImporter.globalScale = .02f;
            modelImporter.importAnimation = false;
            modelImporter.preserveHierarchy = true;
            modelImporter.isReadable = true;
            foreach (var row in report["materials"])
            {
                string sourceName=(string)row["name"], shader=(string)row["shader"];
                string path=materialFolder+"/"+name+"_"+sourceName.Replace(" ","_").Replace(".","_")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                bool additive=shader.Contains("Additive");
                if (material==null)
                {
                    material=additive?new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat")):
                        new Material(Shader.Find(row["baseMap"]!=null?"EmpireAtWar/Ship Lit":"Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(material,path);
                }
                if(row["baseMap"]!=null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["baseMap"]));
                if(row["normalMap"]!=null) { material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["normalMap"]));material.EnableKeyword("_NORMALMAP"); }
                if(row["maskMap"]!=null) { material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["maskMap"])); material.SetFloat("_TeamMaskStrength",1); }
                if(material.HasProperty("_TeamLiveryStrength")) material.SetFloat("_TeamLiveryStrength",0);
                if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",.15f);
                if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.25f);
                EditorUtility.SetDirty(material);
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),sourceName),material);
            }
            modelImporter.SaveAndReimport();
            var visual=new GameObject(name);
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),visual.transform);
                foreach(var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var row=report["source"]["meshes"][renderer.name];
                    if(row==null) throw new InvalidOperationException("No source visibility for "+renderer.name);
                    renderer.enabled=!(bool)row["hidden"];
                }
                if(!name.StartsWith("VictoryIIAdvancedTurret"))
                {
                    var bounds=BoundsOf(visual);
                    float scale=110f/bounds.size.z;
                    model.transform.localScale*=scale;
                    model.transform.localPosition=-bounds.center*scale;
                }
                const string FOLDER="Ships";
                PrefabUtility.SaveAsPrefabAsset(visual,"Assets/Prefabs/Models/"+FOLDER+"/"+name+".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(visual); }
        }
        var hull=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Models/Ships/VictoryIIAdvanced.prefab");
        try
        {
            var rig=hull.transform.GetChild(0);
            for(int i=1;i<=6;i++)
            {
                var bone=rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="T_0"+i);
                var turret=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/VictoryIIAdvancedTurret"+(i==1?"01":"02")+".prefab"),bone);
                turret.name="MediumBurstTurbolaser"+i;
                turret.transform.localPosition=Vector3.zero;
                var turretRig=turret.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith("Rig"));
                turret.transform.localRotation=Quaternion.Inverse(turretRig.localRotation);
                turret.transform.localScale=Vector3.one/turretRig.localScale.x;
            }
            PrefabUtility.SaveAsPrefabAsset(hull,"Assets/Prefabs/Models/Ships/VictoryIIAdvanced.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(hull); }
        AssetDatabase.SaveAssets();
        return "Saved source materials/remaps, 110-unit Victory hull with six separate turrets, using the shared TIE-Interceptor.";
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
    }
}
