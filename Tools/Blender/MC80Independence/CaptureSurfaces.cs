using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CaptureMC80Surfaces
{
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Surface capture requires Edit Mode.");
        var scene=EditorSceneManager.NewPreviewScene();
        var ambient=RenderSettings.ambientLight;
        var mode=RenderSettings.ambientMode;
        var active=RenderTexture.active;
        var palette=Shader.GetGlobalVectorArray("_TeamColors");
        var target=RenderTexture.GetTemporary(1024,1024,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(1024,1024,TextureFormat.RGBA32,false);
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/MC80Independence.prefab"),scene);
            foreach(var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
            var cameraObject=new GameObject("SurfaceCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.enabled=false;
            camera.targetTexture=target; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.035f,.055f,1);
            camera.cullingMask=1<<30; camera.orthographic=true; camera.nearClipPlane=.1f; camera.farClipPlane=1000;
            var lightObject=new GameObject("SurfaceLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
            var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.cullingMask=1<<30;
            light.transform.rotation=Quaternion.Euler(35,30,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
            camera.orthographicSize=125; camera.transform.position=new Vector3(0,300,0); camera.transform.rotation=Quaternion.Euler(90,0,0);
            Render(camera,target,texture,"SurfaceTop");
            Shader.SetGlobalVectorArray("_TeamColors",new[]{new Vector4(.2f,.55f,1,1),new Vector4(1,.25f,.2f,1),new Vector4(.3f,.9f,.4f,1)});
            foreach(var pair in new[]{("Blue",1u),("Red",2u),("Green",3u)})
            {
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(pair.Item2);
                Render(camera,target,texture,"TeamStripeTop"+pair.Item1);
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(0);
            camera.orthographicSize=42; camera.transform.position=new Vector3(65,45,-250); camera.transform.LookAt(new Vector3(0,0,-65));
            Render(camera,target,texture,"SurfaceEngines");
            camera.orthographicSize=30; camera.transform.position=new Vector3(90,60,25); camera.transform.LookAt(new Vector3(0,0,25));
            Render(camera,target,texture,"SurfaceHull");
            camera.orthographicSize=34;camera.transform.position=new Vector3(0,8,-250);camera.transform.LookAt(new Vector3(0,0,-65));
            Render(camera,target,texture,"SurfaceAft");
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.sharedMaterials.Any(material=>material.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
            Render(camera,target,texture,"SurfaceAftNoGlow");
            return "Captured top, hull and aft-engine surfaces at 1024px.";
        }
        finally
        {
            RenderTexture.active=active; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            RenderSettings.ambientLight=ambient; RenderSettings.ambientMode=mode;
            Shader.SetGlobalVectorArray("_TeamColors",palette.Length>0?palette:new Vector4[8]);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    static void Render(Camera camera,RenderTexture target,Texture2D texture,string name)
    {
        camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,1024,1024),0,0); texture.Apply();
        File.WriteAllBytes("Temp/MC80IndependenceImport/Previews/"+name+".png",texture.EncodeToPNG());
    }
}
