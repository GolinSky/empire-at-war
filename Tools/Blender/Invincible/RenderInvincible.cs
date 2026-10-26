using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using EmpireAtWar.Models.Players;

public static class RenderInvincible
{
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/InvincibleWreckView.prefab";
    const string PLACEMENT = "Assets/Prefabs/Ui/Reinforcement/InvincibleReinforcementView.prefab";
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/InvincibleIcon.png";
    const string OUTPUT = "Temp/InvincibleImport/Previews/";
    const int LAYER = 30;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Invincible rendering requires Edit Mode.");
        Directory.CreateDirectory(OUTPUT);
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var ownedMaterials = new List<Material>();
        try
        {
            var root = Geometry(VISUAL, scene);
            var cameraObject = new GameObject("InvinciblePreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .035f, .055f, 0);
            camera.cullingMask = 1 << LAYER;
            camera.orthographic = true;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 5000;
            Light(scene, "InvincibleKey", 1.6f, new Vector3(35, 30, 0));
            Light(scene, "InvincibleFill", .7f, new Vector3(-20, -130, 0));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.4f, .42f, .47f);
            Frame(camera, new[] { root }, new Vector3(50, 60, 70));
            Render(camera, ICON, 512);
            Render(camera, OUTPUT + "Invincible.png", 1024);
            var renderers = Renderers(root).ToArray();
            var originals = renderers.Select(r => r.sharedMaterials).ToArray();
            var white = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            white.SetColor("_BaseColor", Color.white);
            ownedMaterials.Add(white);
            foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(white, renderer.sharedMaterials.Length).ToArray();
            Render(camera, OUTPUT + "Silhouette.png", 512);
            for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = originals[i];
            Frame(camera, new[] { root }, new Vector3(0, 200, .001f));
            Render(camera, OUTPUT + "Top.png", 1024);
            Frame(camera, new[] { root }, new Vector3(50, 60, -70));
            Render(camera, OUTPUT + "Stern.png", 1024);
            Frame(camera, new[] { root }, new Vector3(50, 60, 70));
            var palette = AssetDatabase.LoadAssetAtPath<TeamColorPalette>("Assets/Settings/Data/Models/Players/TeamColorPalette.asset");
            var colors = palette.CreateLinearColors();
            Shader.SetGlobalVectorArray("_TeamColors", colors);
            for (int i = 0; i < colors.Length; i++)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1));
                Render(camera, OUTPUT + "Team" + i + ".png", 768);
            }
            root.SetActive(false);
            var placement = Geometry(PLACEMENT, scene);
            Frame(camera, new[] { placement }, new Vector3(50, 60, 70));
            Render(camera, OUTPUT + "Placement.png", 768);
            placement.SetActive(false);
            var wreck = Geometry(WRECK, scene);
            var halfLength = Points(wreck).Max(p => Mathf.Abs(p.z));
            foreach (var renderer in Renderers(wreck))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source => {
                    var material = new Material(source);
                    ownedMaterials.Add(material);
                    material.SetVector("_WreckAxis", Vector3.forward);
                    material.SetVector("_WreckCenter", Vector3.zero);
                    material.SetVector("_WreckCuts", new Vector4(-halfLength * .25f, halfLength * .25f, 1000000, 1000000));
                    material.SetVector("_WreckAxisRange", new Vector4(-halfLength, halfLength, 0, 0));
                    material.SetFloat("_WreckStartTime", 0);
                    material.SetFloat("_WreckLifetime", 1000000000);
                    material.SetFloat("_WreckGlowDuration", 1000000000);
                    material.SetFloat("_WreckSeparationSpeed", 0);
                    material.SetFloat("_WreckTiltSpeed", 0);
                    material.SetFloat("_WreckSinkSpeed", 0);
                    return material;
                }).ToArray();
            }
            Frame(camera, new[] { wreck }, new Vector3(50, 60, 70));
            for (int i = 0; i < colors.Length; i++)
            {
                foreach (var renderer in Renderers(wreck)) renderer.SetShaderUserValue((uint)(i + 1));
                Render(camera, OUTPUT + "WreckTeam" + i + ".png", 768);
            }
            wreck.SetActive(false);
            root.SetActive(true);
            foreach (var renderer in renderers) renderer.SetShaderUserValue(0);
            root.transform.position = new Vector3(-55, 0, 0);
            var providence = Geometry("Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab", scene);
            var donorPoints = Points(providence);
            var donorCenter = (new Vector3(donorPoints.Min(p => p.x), donorPoints.Min(p => p.y), donorPoints.Min(p => p.z)) + new Vector3(donorPoints.Max(p => p.x), donorPoints.Max(p => p.y), donorPoints.Max(p => p.z))) * .5f;
            providence.transform.position = new Vector3(55, 0, 0) - donorCenter;
            Frame(camera, new[] { root, providence }, new Vector3(0, 200, .001f));
            Render(camera, OUTPUT + "ProvidenceComparison.png", 1024);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in ownedMaterials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors", previousPalette.Length > 0 ? previousPalette : new Vector4[8]);
        }
        AssetDatabase.ImportAsset(ICON, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ICON);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        return "Rendered Invincible icon, silhouette, top/stern/placement, eight live/wreck colors and a Providence size comparison.";
    }

    static GameObject Geometry(string path, Scene scene)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = new GameObject(source.name + "Preview");
        SceneManager.MoveGameObjectToScene(root, scene);
        foreach (var renderer in Renderers(source).Where(r => r.enabled))
        {
            var components = Components(renderer.gameObject).ToArray();
            var filter = components.OfType<MeshFilter>().Single();
            var part = new GameObject(renderer.name);
            part.layer = LAYER;
            part.transform.SetParent(root.transform, false);
            var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            part.transform.localPosition = matrix.GetColumn(3);
            part.transform.localRotation = matrix.rotation;
            part.transform.localScale = matrix.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        return root;
    }
    static Vector3[] Points(GameObject root) => Transforms(root.transform).SelectMany(t => Components(t.gameObject)).OfType<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
    static IEnumerable<MeshRenderer> Renderers(GameObject root) => Transforms(root.transform).SelectMany(t => Components(t.gameObject)).OfType<MeshRenderer>();
    static void Frame(Camera camera, GameObject[] roots, Vector3 direction)
    {
        var points = roots.SelectMany(Points).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        camera.transform.position = bounds.center + direction.normalized * Mathf.Max(bounds.size.magnitude * 3, 200);
        camera.transform.LookAt(bounds.center);
        var projected = points.Select(p => camera.transform.InverseTransformPoint(p)).ToArray();
        camera.orthographicSize = Mathf.Max(projected.Max(p => Mathf.Abs(p.x)), projected.Max(p => Mathf.Abs(p.y))) * 1.12f;
    }
    static void Light(Scene scene, string name, float intensity, Vector3 rotation)
    {
        var owner = new GameObject(name);
        SceneManager.MoveGameObjectToScene(owner, scene);
        var light = owner.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.cullingMask = 1 << LAYER;
        owner.transform.rotation = Quaternion.Euler(rotation);
    }
    static void Render(Camera camera, string path, int size)
    {
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        try
        {
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    static IEnumerable<Transform> Transforms(Transform root)
    {
        yield return root;
        foreach (Transform child in root)
            foreach (var node in Transforms(child)) yield return node;
    }
    static IEnumerable<Component> Components(GameObject obj)
    {
        var array = new SerializedObject(obj).FindProperty("m_Component");
        for (int i = 0; i < array.arraySize; i++) yield return (Component)array.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue;
    }
}
