using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderISDIIReplacement
{
    public static string Main()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var previousPalette=Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient=RenderSettings.ambientLight;
        var previousMode=RenderSettings.ambientMode;
        var oldActive=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDII.prefab"),scene);
        foreach(var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=30;
        var cameraObject=new GameObject("ISDIIPreviewCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
        camera.cullingMask=1<<30; camera.orthographic=true; camera.orthographicSize=100f;
        camera.nearClipPlane=0.01f; camera.farClipPlane=1000;
        camera.transform.position=new Vector3(195,230,260); camera.transform.LookAt(Vector3.zero);
        var lightObject=new GameObject("ISDIIPreviewLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.cullingMask=1<<30;
        light.transform.rotation=Quaternion.Euler(35,30,0);
        var fillObject=new GameObject("ISDIIFillLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);
        var fill=fillObject.AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=0.7f; fill.cullingMask=1<<30;
        fill.transform.rotation=Quaternion.Euler(-20,-130,0);
        var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGB32); camera.targetTexture=target;
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
        var temporaryMaterials=new List<Material>();
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(0.4f,0.42f,0.47f);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Directory.CreateDirectory("Temp/ISDIIReplacement/Previews");
            Render(camera,target,texture,"Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIcon.png");
            var iconPixels=texture.GetPixels32();
            for(int i=0;i<iconPixels.Length;i++) iconPixels[i]=new Color32(255,255,255,iconPixels[i].a);
            texture.SetPixels32(iconPixels); texture.Apply(); File.WriteAllBytes("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIISilhouette.png",texture.EncodeToPNG());
            camera.transform.position=new Vector3(0,200,0); camera.transform.rotation=Quaternion.Euler(90,0,0); Render(camera,target,texture,"Temp/ISDIIReplacement/Previews/Top.png"); camera.transform.position=new Vector3(195,230,-260); camera.transform.LookAt(Vector3.zero); Render(camera,target,texture,"Temp/ISDIIReplacement/Previews/Stern.png"); camera.transform.position=new Vector3(195,230,260); camera.transform.LookAt(Vector3.zero);
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers) renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,"Temp/ISDIIReplacement/Previews/Team"+i+".png");
            }
            Directory.CreateDirectory("output/ISDIIReplacement");
            foreach(var renderer in renderers)renderer.SetShaderUserValue(1u);
            var largeTarget=RenderTexture.GetTemporary(1536,1152,24,RenderTextureFormat.ARGB32);
            var largeTexture=new Texture2D(1536,1152,TextureFormat.RGBA32,false);
            try
            {
                camera.targetTexture=largeTarget;camera.aspect=1536f/1152f;
                Render(camera,largeTarget,largeTexture,"output/ISDIIReplacement/Integrated.png");
                camera.transform.position=new Vector3(0,200,0);camera.transform.rotation=Quaternion.Euler(90,0,0);
                Render(camera,largeTarget,largeTexture,"output/ISDIIReplacement/Top.png");
                camera.transform.position=new Vector3(195,230,260);camera.transform.LookAt(Vector3.zero);
            }
            finally{camera.targetTexture=target;camera.aspect=1;RenderTexture.ReleaseTemporary(largeTarget);UnityEngine.Object.DestroyImmediate(largeTexture);}
            root.SetActive(false);
            foreach(bool wreck in new[]{false,true})
            {
                string path=wreck?"Assets/Prefabs/Models/Wrecks/ISDIIWreckView.prefab":"Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var geometry=new GameObject(wreck?"WreckGeometry":"PlacementGeometry");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(geometry,scene);
                foreach(var source in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
                {
                    var part=new GameObject(source.name);part.layer=30;part.transform.SetParent(geometry.transform,false);
                    var matrix=prefab.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
                    part.transform.localPosition=matrix.GetColumn(3);part.transform.localRotation=matrix.rotation;part.transform.localScale=matrix.lossyScale;
                    part.AddComponent<MeshFilter>().sharedMesh=source.GetComponents<MeshFilter>().Single().sharedMesh;
                    var renderer=part.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials=source.sharedMaterials.Select(m=>
                    {
                        if(!wreck)return m;
                        var clone=new Material(m);temporaryMaterials.Add(clone);
                        clone.SetFloat("_WreckLifetime",1000000000);clone.SetFloat("_WreckGlowDuration",1000000000);
                        clone.SetFloat("_WreckSeparationSpeed",0);clone.SetFloat("_WreckTiltSpeed",0);clone.SetFloat("_WreckSinkSpeed",0);
                        return clone;
                    }).ToArray();
                    renderer.SetShaderUserValue(wreck?1u:0u);
                }
                Render(camera,target,texture,"Temp/ISDIIReplacement/Previews/"+(wreck?"Wreck":"Placement")+".png");
                UnityEngine.Object.DestroyImmediate(geometry);
            }
        }
        finally
        {
            RenderTexture.active=oldActive; camera.targetTexture=null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientMode=previousMode; RenderSettings.ambientLight=previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors",previousPalette.Length>0?previousPalette:new Vector4[8]);
            foreach(var material in temporaryMaterials)UnityEngine.Object.DestroyImmediate(material);
        }
        AssetDatabase.Refresh();
        foreach(var name in new[]{"ISDIIIcon","ISDIISilhouette"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Textures/Ui/Icons/ShipIcon/"+name+".png");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Rendered transparent ISD II icon, silhouette, top/stern, actual placement/wreck geometry and eight team palettes.";
    }
    static void Render(Camera camera,RenderTexture target,Texture2D texture,string path)
    {
        camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
    }
}
