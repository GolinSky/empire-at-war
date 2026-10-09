using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderISDIIEngines
{
    public static string Main()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDII.prefab"),scene);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
        var cameraObject=new GameObject("EngineCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=36;
        camera.nearClipPlane=.1f;camera.farClipPlane=400;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.035f,1);
        var lightObject=new GameObject("EngineLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(15,135,0);
        var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var active=RenderTexture.active;
        var target=RenderTexture.GetTemporary(1200,700,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1200,700,TextureFormat.RGBA32,false);camera.targetTexture=target;
        string folder="Temp/ISDIIReplacement/EngineInspection";Directory.CreateDirectory(folder);
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.25f,.28f,.35f);
            foreach(var pose in new[]{("Rear",new Vector3(0,-11.8f,-160)),("RearTop",new Vector3(50,35,-150)),("RearBottom",new Vector3(45,-50,-155))})
            {
                camera.transform.position=pose.Item2;camera.transform.LookAt(new Vector3(0,-11.8f,-80));
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1200,700),0,0);texture.Apply();
                File.WriteAllBytes(folder+"/"+pose.Item1+".png",texture.EncodeToPNG());
            }
        }
        finally
        {
            RenderTexture.active=active;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;
        }
        return "Rendered rear/top/bottom source engine closeups.";
    }
}
