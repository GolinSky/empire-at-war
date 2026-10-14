using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public static class RenderTectorCircles
{
    public static string Main()
    {
        const string ship="Tector";
        string folder="Tools/Blender/TectorReplacement/Previews/"+ship+"/";Directory.CreateDirectory(folder);
        var scene=EditorSceneManager.NewPreviewScene();var palette=Shader.GetGlobalVectorArray("_TeamColors");var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var active=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab"),scene);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
        var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);float length=bounds.size.z;
        var cameraObject=new GameObject("LiveryCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.cullingMask=1<<30;camera.orthographic=true;camera.orthographicSize=length*.56f;camera.nearClipPlane=.01f;camera.farClipPlane=length*10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.transform.position=new Vector3(1.5f,1.2f,1.5f)*length;camera.transform.LookAt(Vector3.zero);
        foreach(var row in new[]{(new Vector3(35,30,0),1.6f),(new Vector3(-20,-130,0),.7f)}){var go=new GameObject("LiveryLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=row.Item2;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(row.Item1);}
        var target=RenderTexture.GetTemporary(1024,1024,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var texture=new Texture2D(1024,1024,TextureFormat.RGBA32,false);var copies=new List<Material>();
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
            Render(camera,target,texture,folder+"Unowned.png");
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++){foreach(var r in renderers)r.SetShaderUserValue((uint)(i+1));Render(camera,target,texture,folder+"Team"+i+".png");}
            var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float cameraSize=camera.orthographicSize;
            foreach(var r in renderers)r.SetShaderUserValue(1);
            camera.orthographicSize=length*.12f;camera.transform.position=new Vector3(0,length*.8f,length*.28f);camera.transform.LookAt(new Vector3(0,0,length*.08f));
            Render(camera,target,texture,folder+"PaintDetail.png");
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.orthographicSize=cameraSize;
            foreach(var r in renderers)
            {
                if(r.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit")){r.enabled=false;continue;}
                r.sharedMaterials=r.sharedMaterials.Select(m=>{var copy=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/"+ship+"/"+m.name+"_Wreck.mat"));copies.Add(copy);copy.SetVector("_WreckAxis",Vector3.forward);copy.SetVector("_WreckCenter",Vector3.zero);copy.SetVector("_WreckCuts",new Vector4(-length*.1f,length*.14f,1000000,1000000));copy.SetVector("_WreckAxisRange",new Vector4(-length,length,0,0));copy.SetFloat("_WreckStartTime",0);copy.SetFloat("_WreckLifetime",1000000000);copy.SetFloat("_WreckGlowDuration",1000000000);copy.SetFloat("_WreckSeparationSpeed",0);copy.SetFloat("_WreckTiltSpeed",0);copy.SetFloat("_WreckSinkSpeed",0);return copy;}).ToArray();
            }
            for(int i=0;i<8;i++){foreach(var r in renderers.Where(r=>r.enabled))r.SetShaderUserValue((uint)(i+1));Render(camera,target,texture,folder+"WreckTeam"+i+".png");}
        }
        finally
        {
            RenderTexture.active=active;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;Shader.SetGlobalVectorArray("_TeamColors",palette.Length>0?palette:new Vector4[8]);
        }
        AssetDatabase.Refresh();AssetDatabase.SaveAssets();return ship+": eight live/wreck palettes and neutral unowned and paint-detail renders saved.";
    }
    private static void Render(Camera camera,RenderTexture target,Texture2D texture,string path){camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
}
