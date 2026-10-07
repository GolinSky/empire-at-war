using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderExecutor
{
    public static string Main()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var previousPalette=Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient=RenderSettings.ambientLight;
        var previousMode=RenderSettings.ambientMode;
        var oldActive=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Executor.prefab"),scene);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
        var cameraObject=new GameObject("ExecutorPreviewCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
        camera.cullingMask=1<<30; camera.orthographic=true; camera.orthographicSize=1050f;
        camera.nearClipPlane=0.01f; camera.farClipPlane=20000;
        camera.transform.position=new Vector3(2000,2400,2800); camera.transform.LookAt(Vector3.zero);
        var lightObject=new GameObject("ExecutorPreviewLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.cullingMask=1<<30;
        light.transform.rotation=Quaternion.Euler(35,30,0);
        var fillObject=new GameObject("ExecutorFillLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);
        var fill=fillObject.AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=0.7f; fill.cullingMask=1<<30;
        fill.transform.rotation=Quaternion.Euler(-20,-130,0);
        var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGB32); camera.targetTexture=target;
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(0.4f,0.42f,0.47f);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Directory.CreateDirectory("Temp/ExecutorImport/Previews");
            Render(camera,target,texture,"Assets/Art/Textures/Ui/Icons/ShipIcon/ExecutorIcon.png");
            var iconPixels=texture.GetPixels32();
            for(int i=0;i<iconPixels.Length;i++) iconPixels[i]=new Color32(255,255,255,iconPixels[i].a);
            texture.SetPixels32(iconPixels); texture.Apply(); File.WriteAllBytes("Assets/Art/Textures/Ui/Icons/ShipIcon/ExecutorSilhouette.png",texture.EncodeToPNG());
            camera.transform.position=new Vector3(0,4000,0); camera.transform.rotation=Quaternion.Euler(90,0,0); Render(camera,target,texture,"Temp/ExecutorImport/Previews/Top.png"); camera.transform.position=new Vector3(2000,2400,-2800); camera.transform.LookAt(Vector3.zero); Render(camera,target,texture,"Temp/ExecutorImport/Previews/Stern.png"); camera.transform.position=new Vector3(2000,2400,2800); camera.transform.LookAt(Vector3.zero);
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers) renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,"Temp/ExecutorImport/Previews/Team"+i+".png");
            }
        }
        finally
        {
            RenderTexture.active=oldActive; camera.targetTexture=null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientMode=previousMode; RenderSettings.ambientLight=previousAmbient;
            if(previousPalette!=null && previousPalette.Length>0) Shader.SetGlobalVectorArray("_TeamColors",previousPalette);
            else Shader.SetGlobalVectorArray("_TeamColors",new Vector4[8]);
        }
        AssetDatabase.Refresh();
        foreach(var name in new[]{"ExecutorIcon","ExecutorSilhouette"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Textures/Ui/Icons/ShipIcon/"+name+".png");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Rendered transparent Executor icon, silhouette, top/stern views and eight team palettes.";
    }
    static void Render(Camera camera,RenderTexture target,Texture2D texture,string path)
    {
        camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,512,512),0,0); texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
    }
}
