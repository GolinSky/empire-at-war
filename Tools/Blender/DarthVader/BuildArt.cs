using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class VaderArt
{
    private const string STEM = "DarthVaderTIEAdvanced";
    private const string MODEL = "Assets/Art/Models/EmpireShips/DarthVaderTIEAdvanced/DarthVaderTIEAdvanced.fbx";
    private const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/DarthVaderTIEAdvanced/";
    public static object Apply()
    {
        var manifest=JArray.Parse(File.ReadAllText("Temp/VaderImport/Output/UnityMaterials.json"));
        var importer=(ModelImporter)AssetImporter.GetAtPath(MODEL);
        importer.globalScale=1f;importer.useFileScale=false;importer.importAnimation=false;
        foreach(JObject row in manifest)
        {
            if((bool)row["hidden"])continue;
            bool additive=(string)row["shader"]=="MeshAdditive.fx";
            string path=MATERIALS+STEM+"_"+(string)row["material"]+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find(additive?"Universal Render Pipeline/Unlit":"EmpireAtWar/Ship Lit"));AssetDatabase.CreateAsset(mat,path);}
            foreach(var texture in ((JObject)row["textures"]).Properties())
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath((string)texture.Value);
                ti.textureType=texture.Name=="NormalTexture"?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=texture.Name!="NormalTexture";ti.flipGreenChannel=texture.Name=="NormalTexture";ti.SaveAndReimport();
                mat.SetTexture(texture.Name=="NormalTexture"?"_BumpMap":"_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>((string)texture.Value));
            }
            float tint=(int)row["index"]==3?0.10588236f:1f;
            mat.SetColor("_BaseColor",new Color(tint,tint,tint,1f));
            if(additive)
            {
                mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",2);mat.SetFloat("_SrcBlend",(float)BlendMode.One);mat.SetFloat("_DstBlend",(float)BlendMode.One);
                mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",(float)CullMode.Off);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;mat.SetOverrideTag("RenderType","Transparent");
            }
            else{mat.SetFloat("_Metallic",0.15f);mat.SetFloat("_Smoothness",0.35f);mat.SetFloat("_TeamLiveryStrength",0f);}
            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),(string)row["material"]),mat);
        }
        importer.SaveAndReimport();
        string icon="Assets/Art/Textures/Ui/Icons/SquadronIcon/Empire/"+STEM+"Icon.png";
        var iconImporter=(TextureImporter)AssetImporter.GetAtPath(icon);iconImporter.textureType=TextureImporterType.Sprite;iconImporter.spriteImportMode=SpriteImportMode.Single;iconImporter.alphaIsTransparency=true;iconImporter.mipmapEnabled=false;iconImporter.SaveAndReimport();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var root=new GameObject(STEM);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL),root.transform);
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var components=BalancePrefabBindings.Components(model).OfType<Component>().ToArray();
            var hiddenNames=manifest.Where(r=>(bool)r["hidden"]).Select(r=>(string)r["mesh"]).ToHashSet();
            foreach(var renderer in components.OfType<MeshRenderer>().Where(r=>hiddenNames.Contains(r.name)).ToArray())
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var renderers=BalancePrefabBindings.Components(model).OfType<MeshRenderer>().ToArray();
            var points=BalancePrefabBindings.Components(model).OfType<MeshFilter>().SelectMany(f=>
            {
                var b=f.sharedMesh.bounds;
                return new[]{-1f,1f}.SelectMany(x=>new[]{-1f,1f}.SelectMany(y=>new[]{-1f,1f}.Select(z=>f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))))));
            }).ToArray();
            Bounds bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points.Skip(1))bounds.Encapsulate(point);
            float scale=6f/bounds.size.z;
            model.transform.localScale*=scale;model.transform.localPosition=-bounds.center*scale;
            foreach(var r in renderers){r.enabled=true;r.shadowCastingMode=ShadowCastingMode.Off;}
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Models/Squadrons/"+STEM+".prefab");
            AssetDatabase.SaveAssets();
            return new{sourceBounds=bounds.ToString(),scale,renderers=renderers.Length};
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }
}
