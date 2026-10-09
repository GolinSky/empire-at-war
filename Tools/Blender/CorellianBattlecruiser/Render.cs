using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderCorellianBattlecruiser
{
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianBattlecruiserIcon.png";
    const string PREVIEWS = "Temp/CorellianBattlecruiserImport/Previews/";
    public static string Main()
    {
        Directory.CreateDirectory(PREVIEWS);
        var scene=EditorSceneManager.NewPreviewScene();
        var previousPalette=Shader.GetGlobalVectorArray("_TeamColors") ?? new Vector4[8];
        var previousAmbient=RenderSettings.ambientLight;
        var previousMode=RenderSettings.ambientMode;
        var oldActive=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/CorellianBattlecruiser.prefab"),scene);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
        var cameraObject=new GameObject("CorellianPreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.cullingMask=1<<30;camera.orthographic=true;camera.orthographicSize=78;
        camera.nearClipPlane=.1f;camera.farClipPlane=1000;
        camera.transform.position=new Vector3(190,150,190);camera.transform.LookAt(Vector3.zero);
        var lightObject=new GameObject("CorellianPreviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<30;
        light.transform.rotation=Quaternion.Euler(35,30,0);
        var fillObject=new GameObject("CorellianFillLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);
        var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.cullingMask=1<<30;
        fill.transform.rotation=Quaternion.Euler(-20,-130,0);
        var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
        var wreckMaterials=new List<Material>();
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Render(camera,target,texture,ICON);
            var pixels=texture.GetPixels32();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,pixels[i].a);
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(ICON.Replace("Icon.png","Silhouette.png"),texture.EncodeToPNG());
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers)renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,PREVIEWS+"Team"+i+".png");
            }
            foreach(var renderer in renderers)renderer.SetShaderUserValue(0);
            var originals=renderers.Select(r=>r.sharedMaterials).ToArray();
            var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)
            {
                if(renderer.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"){renderer.enabled=false;continue;}
                renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
            }
            Render(camera,target,texture,PREVIEWS+"Placement.png");
            for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originals[i];
            foreach(var renderer in renderers)
            {
                if(renderer.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"){renderer.enabled=false;continue;}
                var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/CorellianBattlecruiser/"+renderer.sharedMaterial.name+"_Wreck.mat"));
                wreckMaterials.Add(material);
                // Freeze the fresh wreck preview; runtime timing comes from UnitWreckView.Show.
                material.SetVector("_WreckAxis",Vector3.forward);
                material.SetVector("_WreckCenter",Vector3.zero);
                material.SetVector("_WreckCuts",new Vector4(-18,20,1000000,1000000));
                material.SetVector("_WreckAxisRange",new Vector4(-80,80,0,0));
                material.SetFloat("_WreckStartTime",0);
                material.SetFloat("_WreckLifetime",1000000000);
                material.SetFloat("_WreckGlowDuration",1000000000);
                material.SetFloat("_WreckSeparationSpeed",0);
                material.SetFloat("_WreckTiltSpeed",0);
                material.SetFloat("_WreckSinkSpeed",0);
                renderer.sharedMaterial=material;
                renderer.SetShaderUserValue(1);
            }
            Render(camera,target,texture,PREVIEWS+"Wreck.png");
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers.Where(r=>r.enabled))renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,PREVIEWS+"WreckTeam"+i+".png");
            }
        }
        finally
        {
            RenderTexture.active=oldActive;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);
            foreach(var material in wreckMaterials)UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode=previousMode;RenderSettings.ambientLight=previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors",previousPalette.Length>0?previousPalette:new Vector4[8]);
        }
        AssetDatabase.Refresh();
        foreach(var path in new[]{ICON,ICON.Replace("Icon.png","Silhouette.png")})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();return "Corellian icon, silhouette, eight team palettes, placement and wreck rendered.";
    }
    static void Render(Camera camera,RenderTexture target,Texture2D texture,string path)
    {
        camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
    }
}
