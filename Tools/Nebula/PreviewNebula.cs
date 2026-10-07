using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewNebula
{
    public static void Run(string label = "preview")
    {
        const string OUTPUT = "output/NebulaRebuild/";
        Directory.CreateDirectory(OUTPUT);
        var scene = EditorSceneManager.NewPreviewScene();
        var previous = RenderTexture.active;
        var target = new RenderTexture(1600,900,24,RenderTextureFormat.ARGBHalf);
        var image = new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            var cameraRoot = Clone(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/Camera/Main Camera.prefab"),null,scene);
            var camera = cameraRoot.GetComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.transform.position = new Vector3(0,500,-1600);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = target;
            var data = cameraRoot.GetComponent<UniversalAdditionalCameraData>();
            data.requiresDepthTexture = true;
            data.renderPostProcessing = true;
            var volumeObject = new GameObject("Preview Volume");
            SceneManager.MoveGameObjectToScene(volumeObject,scene);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Scenes/Planets/Corusant/Global Volume Profile.asset");
            var lightObject = new GameObject("Preview Sun");
            SceneManager.MoveGameObjectToScene(lightObject,scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(30,-30,0);
            var background = Clone(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vfx/SkirmishVfx.prefab"),null,scene);
            var renderer = background.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Nebula Volume");
            foreach (var stars in background.GetComponentsInChildren<ParticleSystem>())
            {
                stars.useAutoRandomSeed = false;
                stars.randomSeed = 7201;
                stars.Simulate(0.1f,false,true,false);
            }
            var planet = Clone(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/Planets/CoruscantPlanet.prefab"),null,scene);
            planet.transform.localPosition *= 6;
            planet.transform.localScale *= 6;
            foreach (string variant in new[] { "AzureRift", "VioletPillars", "EmberWings" })
            {
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Nebula/" + variant + ".mat");
                foreach (string framing in new[] { "background", "planet", "pan" })
                {
                    planet.SetActive(framing != "background");
                    camera.transform.position = framing == "pan" ? new Vector3(4000,940,-4500) : new Vector3(0,500,-1600);
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                    image.Apply();
                    File.WriteAllBytes(OUTPUT + label + "-" + variant + "-" + framing + ".png",image.EncodeToPNG());
                }
            }
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
    }

    private static GameObject Clone(GameObject source, Transform parent, Scene scene)
    {
        var clone = new GameObject(source.name);
        clone.SetActive(false);
        clone.layer = source.layer;
        SceneManager.MoveGameObjectToScene(clone,scene);
        clone.transform.SetParent(parent,false);
        clone.transform.localPosition = source.transform.localPosition;
        clone.transform.localRotation = source.transform.localRotation;
        clone.transform.localScale = source.transform.localScale;
        foreach (var component in source.GetComponents<Component>())
        {
            if (!(component is MeshFilter || component is MeshRenderer || component is ParticleSystem || component is ParticleSystemRenderer || component is Camera || component is UniversalAdditionalCameraData)) continue;
            var destination = clone.GetComponent(component.GetType());
            if (destination == null) destination = clone.AddComponent(component.GetType());
            EditorUtility.CopySerialized(component,destination);
            if (destination is Camera camera) camera.enabled = false;
        }
        foreach (Transform child in source.transform) Clone(child.gameObject,clone.transform,scene);
        clone.SetActive(source.activeSelf);
        return clone;
    }
}
