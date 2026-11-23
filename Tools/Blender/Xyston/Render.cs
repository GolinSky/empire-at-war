using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders the Xyston icon with the ISD I Remake camera/lights, plus review previews (top, stern, side, underside,
/// placement and eight live/wreck team palettes) into Temp/XystonImport/Previews.
/// Run: unity command script run --file Tools/Blender/Xyston/Render.cs --entry RenderXyston.Main
/// </summary>
public static class RenderXyston
{
    private const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/XystonIcon.png";
    private const string PREVIEWS = "Temp/XystonImport/Previews/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Xyston rendering requires Edit Mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var oldActive = RenderTexture.active;
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/Models/Xyston.prefab"), scene);
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
        var cameraObject = new GameObject("XystonPreviewCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << 30; camera.orthographic = true;
        // ISD I icon framing (size 100 for 168 units), scaled to the 2,406 m (252.63-unit) hull.
        camera.orthographicSize = 100f * (2406f * 0.105f) / 168f;
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 1000;
        var lightObject = new GameObject("XystonPreviewLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f; light.cullingMask = 1 << 30;
        light.transform.rotation = Quaternion.Euler(35, 30, 0);
        var fillObject = new GameObject("XystonFillLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject, scene);
        var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.7f; fill.cullingMask = 1 << 30;
        fill.transform.rotation = Quaternion.Euler(-20, -130, 0);
        var target = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        var wreckMaterials = new System.Collections.Generic.List<Material>();
        try
        {
            Directory.CreateDirectory(PREVIEWS);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            View(camera, new Vector3(240, 280, 330)); Render(camera, target, texture, ICON);
            camera.transform.position = new Vector3(0, 500, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            Render(camera, target, texture, PREVIEWS + "Top.png");
            camera.transform.position = new Vector3(0, -500, 0); camera.transform.rotation = Quaternion.Euler(-90, 0, 0);
            Render(camera, target, texture, PREVIEWS + "Bottom.png");
            View(camera, new Vector3(500, 0, 0)); Render(camera, target, texture, PREVIEWS + "Side.png");
            View(camera, new Vector3(240, -200, 330)); Render(camera, target, texture, PREVIEWS + "Below.png");
            View(camera, new Vector3(240, 280, -330)); Render(camera, target, texture, PREVIEWS + "Stern.png");
            View(camera, new Vector3(240, 280, 330));
            Color[] colors = { new Color(.2f, .55f, 1), new Color(1, .25f, .2f), new Color(.3f, .9f, .4f), new Color(1, .75f, .2f), new Color(.7f, .35f, 1), new Color(.2f, .9f, .9f), new Color(1, .45f, .75f), new Color(.9f, .9f, .9f) };
            Shader.SetGlobalVectorArray("_TeamColors", colors.Select(c => (Vector4)c).ToArray());
            for (int i = 0; i < 8; i++)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1));
                Render(camera, target, texture, PREVIEWS + "Team" + i + ".png");
            }
            root.SetActive(false);
            var placement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab"), scene);
            foreach (var transform in placement.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            Render(camera, target, texture, PREVIEWS + "Placement.png");
            placement.SetActive(false);
            var wreck = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/XystonWreckView.prefab"), scene);
            foreach (var transform in wreck.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            renderers = wreck.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    var material = new Material(source); wreckMaterials.Add(material);
                    material.SetFloat("_WreckLifetime", 1000000); material.SetFloat("_WreckSeparationSpeed", 0);
                    material.SetFloat("_WreckTiltSpeed", 0); material.SetFloat("_WreckSinkSpeed", 0);
                    material.SetVector("_WreckCuts", Vector4.one * 1000000);
                    return material;
                }).ToArray();
            }
            for (int i = 0; i < 8; i += 7)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1));
                Render(camera, target, texture, PREVIEWS + "WreckTeam" + i + ".png");
            }
        }
        finally
        {
            RenderTexture.active = oldActive; camera.targetTexture = null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in wreckMaterials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode = previousMode; RenderSettings.ambientLight = previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors", previousPalette.Length > 0 ? previousPalette : new Vector4[8]);
        }
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return "Rendered Xyston icon and previews to " + PREVIEWS;
    }

    private static void View(Camera camera, Vector3 position)
    {
        camera.transform.position = position;
        camera.transform.LookAt(Vector3.zero);
    }

    private static void Render(Camera camera, RenderTexture target, Texture2D texture, string path)
    {
        camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }
}
