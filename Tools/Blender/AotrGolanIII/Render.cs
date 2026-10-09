using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderGolan
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var palette = Shader.GetGlobalVectorArray("_TeamColors");
        var ambient = RenderSettings.ambientLight;
        var mode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var cameraObject = new GameObject("GolanPreviewCamera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene=scene; camera.enabled=false; camera.orthographic=true;
        camera.orthographicSize=190; camera.nearClipPlane=.1f; camera.farClipPlane=2000;
        camera.cullingMask=1<<30; camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.025f,.035f,.055f,1);
        camera.transform.position=new Vector3(350,380,450); camera.transform.LookAt(Vector3.zero);
        var lightObject=new GameObject("GolanPreviewLight");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
        var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.8f; light.cullingMask=1<<30;
        light.transform.rotation=Quaternion.Euler(35,30,0);
        var target=RenderTexture.GetTemporary(900,900,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(900,900,TextureFormat.RGBA32,false);
        camera.targetTexture=target;
        Directory.CreateDirectory("Temp/AotrGolanIIIImport/Previews");
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.52f,.57f);
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatform.prefab"),scene);
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=30;
            for(int team=-1;team<8;team++)
            {
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue((uint)(team+1));
                camera.Render(); RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,900,900),0,0); texture.Apply();
                File.WriteAllBytes("Temp/AotrGolanIIIImport/Previews/"+(team<0?"GolanIII":"Team"+team)+".png",texture.EncodeToPNG());
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(0);
            camera.backgroundColor=Color.clear;
            camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,900,900),0,0); texture.Apply();
            const string ICON="Assets/Art/Textures/Ui/Icons/Ui/AotrGolanIIIDefensePlatformIcon.png";
            File.WriteAllBytes(ICON,texture.EncodeToPNG());
            AssetDatabase.ImportAsset(ICON);
            var importer=(TextureImporter)AssetImporter.GetAtPath(ICON);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        }
        finally
        {
            RenderTexture.active=previousTarget; camera.targetTexture=null;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientLight=ambient; RenderSettings.ambientMode=mode;
            if(palette.Length>0) Shader.SetGlobalVectorArray("_TeamColors",palette);
        }
        return "Rendered original and eight team palettes in Unity; saved platform sprite.";
    }
}
