using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RenderTibanna
{
    const string OUTPUT = "output/TibannaGasStationImport/Previews/";
    const string ICON = "Assets/Art/Textures/Ui/Icons/Ui/TibannaGasStationIcon.png";
    public static string Main()
    {
        Directory.CreateDirectory(OUTPUT);
        var scene = EditorSceneManager.NewPreviewScene();
        var palette = Shader.GetGlobalVectorArray("_TeamColors"); var ambient = RenderSettings.ambientLight; var mode = RenderSettings.ambientMode; var previousTarget = RenderTexture.active;
        var root = new GameObject("Preview"); SceneManager.MoveGameObjectToScene(root, scene);
        var cameraObject = new GameObject("Camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false; camera.orthographic = true;
        camera.nearClipPlane = .01f; camera.farClipPlane = 1000; camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .045f, .065f, 1);
        foreach (var direction in new[] {new Vector3(35, 30, 0), new Vector3(-20, -130, 0)})
        {
            var go = new GameObject("Light"); SceneManager.MoveGameObjectToScene(go, scene); var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = direction.x > 0 ? 1.4f : .7f; light.cullingMask = 1 << 30; go.transform.rotation = Quaternion.Euler(direction);
        }
        var target = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target; var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .42f, .47f);
            Shader.SetGlobalVectorArray("_TeamColors", new[] {new Vector4(.2f,.55f,1,1),new Vector4(.3f,.9f,.4f,1),new Vector4(1,.25f,.2f,1),new Vector4(1,.75f,.2f,1),new Vector4(.7f,.35f,1,1),new Vector4(.2f,.9f,.9f,1),new Vector4(1,.45f,.75f,1),new Vector4(.9f,.9f,.9f,1)});
            foreach (string path in new[] {"Assets/Prefabs/Models/MiningFacilities/TibannaGasStation.prefab", "Assets/Prefabs/Ui/Reinforcement/TibannaGasStationReinforcementView.prefab", "Assets/Prefabs/Models/Wrecks/TibannaGasStationWreckView.prefab"})
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
                {
                    var go = new GameObject(renderer.name); go.layer = 30; go.transform.SetParent(root.transform, false); go.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation); go.transform.localScale = renderer.transform.lossyScale;
                    go.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponents<MeshFilter>().Single().sharedMesh; go.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
                var renderers = root.GetComponentsInChildren<MeshRenderer>(); var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var wreckMaterials = new System.Collections.Generic.List<Material>();
                if (path.Contains("Wreck")) foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(sourceMaterial => {var material = new Material(sourceMaterial); wreckMaterials.Add(material); material.SetFloat("_WreckLifetime", 1000000); material.SetFloat("_WreckSeparationSpeed",0); material.SetFloat("_WreckTiltSpeed",0); material.SetFloat("_WreckSinkSpeed",0); material.SetVector("_WreckCuts",Vector4.one*1000000); return material;}).ToArray();
                camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * .72f; camera.transform.position = bounds.center + new Vector3(1,1.2f,-1.4f) * 100; camera.transform.LookAt(bounds.center);
                camera.backgroundColor = new Color(.035f,.045f,.065f,1);
                string name = Path.GetFileNameWithoutExtension(path); Render(camera,target,texture,OUTPUT+name+".png");
                if (name == "TibannaGasStation")
                {
                    camera.backgroundColor = Color.clear; Render(camera,target,texture,ICON); camera.backgroundColor = new Color(.035f,.045f,.065f,1);
                    for (int team = 0; team < 8; team++) {foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(team+1)); Render(camera,target,texture,OUTPUT+"Team"+team+".png");}
                }
                foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach (var material in wreckMaterials) UnityEngine.Object.DestroyImmediate(material);
            }
            AssetDatabase.ImportAsset(ICON); var importer = (TextureImporter)AssetImporter.GetAtPath(ICON); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        }
        finally
        {
            RenderTexture.active = previousTarget; camera.targetTexture = null; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene); RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = mode; Shader.SetGlobalVectorArray("_TeamColors", palette != null && palette.Length > 0 ? palette : new Vector4[8]);
        }
        return "Saved actual-model512px transparent icon, eight team previews, wreck and placement preview.";
    }
    static void Render(Camera camera, RenderTexture target, Texture2D texture, string path) {camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,512,512),0,0); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG());}
}
