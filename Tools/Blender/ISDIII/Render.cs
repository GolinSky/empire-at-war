using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderISDIII
{
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("ISD III preview rendering requires Edit Mode.");
        var scene=EditorSceneManager.NewPreviewScene();
        var previousPalette=Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient=RenderSettings.ambientLight;
        var previousMode=RenderSettings.ambientMode;
        var oldActive=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIII.prefab"),scene);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
        var cameraObject=new GameObject("ISDIIIPreviewCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
        camera.cullingMask=1<<30; camera.orthographic=true; camera.orthographicSize=110f;
        camera.nearClipPlane=0.01f; camera.farClipPlane=1000;
        camera.transform.position=new Vector3(240,280,330); camera.transform.LookAt(Vector3.zero);
        var lightObject=new GameObject("ISDIIIPreviewLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.cullingMask=1<<30;
        light.transform.rotation=Quaternion.Euler(35,30,0);
        var fillObject=new GameObject("ISDIIIFillLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);
        var fill=fillObject.AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=0.7f; fill.cullingMask=1<<30;
        fill.transform.rotation=Quaternion.Euler(-20,-130,0);
        var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGB32); camera.targetTexture=target;
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
        var wreckMaterials=new System.Collections.Generic.List<Material>();
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(0.4f,0.42f,0.47f);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Directory.CreateDirectory("Temp/ISDIIIImport/Previews");
            Render(camera,target,texture,"Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIIcon.png");
            var iconPixels=texture.GetPixels32();
            for(int i=0;i<iconPixels.Length;i++) iconPixels[i]=new Color32(255,255,255,iconPixels[i].a);
            texture.SetPixels32(iconPixels); texture.Apply(); File.WriteAllBytes("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIISilhouette.png",texture.EncodeToPNG());
            camera.transform.position=new Vector3(0,500,0); camera.transform.rotation=Quaternion.Euler(90,0,0); Render(camera,target,texture,"Temp/ISDIIIImport/Previews/Top.png"); camera.transform.position=new Vector3(240,280,-330); camera.transform.LookAt(Vector3.zero); Render(camera,target,texture,"Temp/ISDIIIImport/Previews/Stern.png"); camera.transform.position=new Vector3(240,280,330); camera.transform.LookAt(Vector3.zero);
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers) renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,"Temp/ISDIIIImport/Previews/Team"+i+".png");
            }
            root.SetActive(false);
            var placement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ISDIIIReinforcementView.prefab"),scene);
            foreach(var transform in placement.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
            Render(camera,target,texture,"Temp/ISDIIIImport/Previews/Placement.png");
            placement.SetActive(false);
            var wreck=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/ISDIIIWreckView.prefab"),scene);
            foreach(var transform in wreck.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
            renderers=wreck.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
                {
                    var material=new Material(source); wreckMaterials.Add(material);
                    material.SetFloat("_WreckLifetime",1000000); material.SetFloat("_WreckSeparationSpeed",0);
                    material.SetFloat("_WreckTiltSpeed",0); material.SetFloat("_WreckSinkSpeed",0);
                    material.SetVector("_WreckCuts",Vector4.one*1000000);
                    return material;
                }).ToArray();
            }
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers) renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,"Temp/ISDIIIImport/Previews/WreckTeam"+i+".png");
            }
        }
        finally
        {
            RenderTexture.active=oldActive; camera.targetTexture=null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            foreach(var material in wreckMaterials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode=previousMode; RenderSettings.ambientLight=previousAmbient;
            if(previousPalette.Length>0) Shader.SetGlobalVectorArray("_TeamColors",previousPalette);
        }
        AssetDatabase.Refresh();
        foreach(var name in new[]{"ISDIIIIcon","ISDIIISilhouette"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Textures/Ui/Icons/ShipIcon/"+name+".png");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Rendered ISDIII icon, silhouette, placement, top/stern views and eight live/wreck team palettes.";
    }
    static void Render(Camera camera,RenderTexture target,Texture2D texture,string path)
    {
        camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,512,512),0,0); texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
    }
}
