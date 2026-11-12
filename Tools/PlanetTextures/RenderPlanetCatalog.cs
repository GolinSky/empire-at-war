using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RenderPlanetCatalog
{
    private const int TILE_SIZE = 512;
    private const int COLUMNS = 3;
    private const int PREVIEW_LAYER = 30;

    public static string Main(string phase = "catalog", string[] names = null, bool updateMenuPreviews = false)
    {
        if (names == null) names = new[] { "Coruscant", "Kamino", "Alderaan", "Bespin", "Geonosis", "Kuat", "Mustafar", "Naboo", "Kessel" };
        string folder = "Temp/PlanetImport/Renders/" + phase;
        int width = updateMenuPreviews ? 1344 : TILE_SIZE;
        int height = updateMenuPreviews ? 564 : TILE_SIZE;
        Directory.CreateDirectory(folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (updateMenuPreviews) background.LoadImage(File.ReadAllBytes("Tools/PlanetTextures/PreviewBackground.png"));
        var backdropMesh = new Mesh { vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) },
            uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
        var backdropMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        backdropMaterial.SetTexture("_BaseMap", background);
        int rows = (names.Length + COLUMNS - 1) / COLUMNS;
        var sheet = new Texture2D(width * COLUMNS, height * rows, TextureFormat.RGBA32, false);
        var records = new List<object>();
        try
        {
            var cameraObject = new GameObject("PlanetPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.cullingMask = 1 << PREVIEW_LAYER;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.035f, 1);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10000;
            camera.targetTexture = target;
            var backdrop = new GameObject("PreviewBackdrop") { layer = PREVIEW_LAYER };
            SceneManager.MoveGameObjectToScene(backdrop, scene);
            backdrop.AddComponent<MeshFilter>().sharedMesh = backdropMesh;
            backdrop.AddComponent<MeshRenderer>().sharedMaterial = backdropMaterial;
            backdrop.SetActive(updateMenuPreviews);
            var lightObject = new GameObject("PlanetPreviewLight");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = updateMenuPreviews ? 2.5f : 1.5f;
            light.cullingMask = 1 << PREVIEW_LAYER;
            light.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -0.4f, 1));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.19f, 0.23f);
            for (int index = 0; index < names.Length; index++)
            {
                string path = "Assets/Prefabs/View/Planets/" + names[index] + "Planet.prefab";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null) throw new FileNotFoundException("Planet prefab is not imported", path);
                var renderers = new List<MeshRenderer>();
                var visual = CopyVisuals(source.transform, null, scene, renderers);
                try
                {
                    visual.transform.position = Vector3.zero;
                    visual.transform.localScale = Vector3.one;
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float radius = bounds.extents.magnitude;
                    camera.orthographicSize = radius * 0.72f;
                    camera.transform.position = bounds.center + new Vector3(0.2f, 0.35f, -1).normalized * radius * 4;
                    camera.transform.LookAt(bounds.center);
                    if (updateMenuPreviews)
                    {
                        // Match the authored Coruscant/Kamino cards: 69% height, slightly right of center.
                        var surface = renderers.Find(renderer => renderer.name == "Planet");
                        float bodyRadius = surface.transform.lossyScale.x * 0.5f;
                        camera.orthographicSize = bodyRadius / 0.69f;
                        camera.transform.position -= camera.transform.right * camera.orthographicSize * 2 * camera.aspect * 0.043f;
                        backdrop.transform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * 3000, camera.transform.rotation);
                        backdrop.transform.localScale = new Vector3(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2, 1);
                    }
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/" + names[index] + ".png", image.EncodeToPNG());
                    if (updateMenuPreviews)
                    {
                        string preview = "Assets/Art/Textures/Ui/PlanetPreviews/Original/" + names[index] + "_Preview.png";
                        string replacement = folder + "/" + names[index] + ".replacement";
                        File.WriteAllBytes(replacement, image.EncodeToPNG());
                        // Unity's background importer can briefly memory-map the previous PNG on Windows.
                        for (int attempt = 0; ; attempt++)
                        {
                            try { File.Replace(replacement, preview, null); break; }
                            catch (IOException) when (attempt < 10) { System.Threading.Thread.Sleep(200); }
                        }
                        AssetDatabase.ImportAsset(preview, ImportAssetOptions.ForceSynchronousImport);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(preview);
                        importer.maxTextureSize = 2048;
                        var platform = importer.GetPlatformTextureSettings("Standalone");
                        platform.maxTextureSize = 2048;
                        importer.SetPlatformTextureSettings(platform);
                        importer.SaveAndReimport();
                    }
                    sheet.SetPixels(index % COLUMNS * width, (rows - 1 - index / COLUMNS) * height,
                        width, height, image.GetPixels());
                    records.Add(new { name = names[index], prefab = path, renderers = renderers.Count,
                        bounds = bounds.ToString(), camera = camera.transform.position.ToString(), camera.orthographicSize });
                }
                finally { Object.DestroyImmediate(visual); }
            }
            sheet.Apply();
            File.WriteAllBytes(folder + "/ContactSheet.png", sheet.EncodeToPNG());
            File.WriteAllText(folder + "/Capture.json", JsonConvert.SerializeObject(records, Formatting.Indented));
            if (updateMenuPreviews) AssetDatabase.SaveAssets();
        }
        finally
        {
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(background);
            Object.DestroyImmediate(backdropMesh);
            Object.DestroyImmediate(backdropMaterial);
            Object.DestroyImmediate(sheet);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousAmbient;
        }
        return folder;
    }

    // Copy only visual data: no scopes, game lifecycle callbacks, superweapon registry or lens flares.
    private static GameObject CopyVisuals(Transform source, Transform parent, Scene scene, List<MeshRenderer> renderers)
    {
        var copy = new GameObject(source.name) { layer = PREVIEW_LAYER };
        SceneManager.MoveGameObjectToScene(copy, scene);
        copy.transform.SetParent(parent, false);
        copy.transform.localPosition = source.localPosition;
        copy.transform.localRotation = source.localRotation;
        copy.transform.localScale = source.localScale;
        using (var serialized = new SerializedObject(source.gameObject))
        {
            var components = serialized.FindProperty("m_Component");
            MeshFilter mesh = null;
            MeshRenderer renderer = null;
            for (int i = 0; i < components.arraySize; i++)
            {
                var component = components.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue;
                if (component is MeshFilter filter) mesh = filter;
                if (component is MeshRenderer candidate) renderer = candidate;
            }
            if (renderer != null && renderer.enabled)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var rendered = copy.AddComponent<MeshRenderer>();
                rendered.sharedMaterials = renderer.sharedMaterials;
                rendered.shadowCastingMode = ShadowCastingMode.Off;
                rendered.receiveShadows = false;
                renderers.Add(rendered);
            }
        }
        for (int i = 0; i < source.childCount; i++)
        {
            var child = source.GetChild(i);
            if (child.gameObject.activeSelf) CopyVisuals(child, copy.transform, scene, renderers);
        }
        return copy;
    }
}
