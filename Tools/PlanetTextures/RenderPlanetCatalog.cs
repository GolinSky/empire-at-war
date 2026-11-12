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
        Directory.CreateDirectory(folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var target = RenderTexture.GetTemporary(TILE_SIZE, TILE_SIZE, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(TILE_SIZE, TILE_SIZE, TextureFormat.RGBA32, false);
        int rows = (names.Length + COLUMNS - 1) / COLUMNS;
        var sheet = new Texture2D(TILE_SIZE * COLUMNS, TILE_SIZE * rows, TextureFormat.RGBA32, false);
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
            var lightObject = new GameObject("PlanetPreviewLight");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
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
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, TILE_SIZE, TILE_SIZE), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/" + names[index] + ".png", image.EncodeToPNG());
                    if (updateMenuPreviews)
                    {
                        var imports = JObject.Parse(File.ReadAllText("Temp/PlanetImport/imported-textures.json"));
                        string preview = (string)imports["previews"][names[index]];
                        File.WriteAllBytes(preview, image.EncodeToPNG());
                        AssetDatabase.ImportAsset(preview, ImportAssetOptions.ForceSynchronousImport);
                    }
                    sheet.SetPixels(index % COLUMNS * TILE_SIZE, (rows - 1 - index / COLUMNS) * TILE_SIZE,
                        TILE_SIZE, TILE_SIZE, image.GetPixels());
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
