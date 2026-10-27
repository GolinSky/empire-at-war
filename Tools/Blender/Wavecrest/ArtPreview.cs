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

public static class RenderWavecrestArtPreview
{
    const string ICON = "Temp/WavecrestImport/Previews/ArtIcon.png";
    const string PREVIEWS = "Temp/WavecrestImport/Previews/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Wavecrest.prefab";
    const int SIZE = 512;
    const int LAYER = 30;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Wavecrest preview rendering requires Edit Mode.");
        Directory.CreateDirectory(PREVIEWS);
        Directory.CreateDirectory(Path.GetDirectoryName(ICON));
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousActive = RenderTexture.active;
        var ownedMaterials = new List<Material>();
        var root = Geometry(VISUAL, scene);
        var cameraObject = new GameObject("WavecrestPreviewCamera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << LAYER; camera.orthographic = true;
        camera.nearClipPlane = .01f; camera.farClipPlane = 1000;
        Light(scene, "WavecrestKey", 1.6f, new Vector3(35, 30, 0));
        Light(scene, "WavecrestFill", .7f, new Vector3(-20, -130, 0));
        var target = RenderTexture.GetTemporary(SIZE, SIZE, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.4f, .42f, .47f);
            Frame(camera, root, new Vector3(50, 60, 70));
            Render(camera, target, texture, ICON);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var originalMaterials = renderers.Select(r => r.sharedMaterials).ToArray();
            var white = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            white.SetColor("_BaseColor", Color.white); ownedMaterials.Add(white);
            foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(white, renderer.sharedMaterials.Length).ToArray();
            Render(camera, target, texture, PREVIEWS + "Silhouette.png");
            for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = originalMaterials[i];
            Frame(camera, root, new Vector3(200, 0, .001f)); Render(camera, target, texture, PREVIEWS + "Side.png");
            Frame(camera, root, new Vector3(0, 200, .001f)); Render(camera, target, texture, PREVIEWS + "Top.png");
            Frame(camera, root, new Vector3(50, 60, -70)); Render(camera, target, texture, PREVIEWS + "Stern.png");
            Frame(camera, root, new Vector3(50, 60, 70));
            var palette = AssetDatabase.LoadAssetAtPath<TeamColorPalette>("Assets/Settings/Data/Models/Players/TeamColorPalette.asset");
            var colors = palette.CreateLinearColors();
            Shader.SetGlobalVectorArray("_TeamColors", colors);
            for (int i = 0; i < colors.Length; i++)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1));
                Render(camera, target, texture, PREVIEWS + "Team" + i + ".png");
            }

        }
        finally
        {
            RenderTexture.active = previousActive; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in ownedMaterials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode = previousMode; RenderSettings.ambientLight = previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors", previousPalette.Length > 0 ? previousPalette : new Vector4[8]);
        }
        return "Rendered Wavecrest art only.";
    }

    static GameObject Geometry(string path, Scene scene)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = new GameObject(source.name + "Preview"); SceneManager.MoveGameObjectToScene(root, scene);
        // Copy geometry only: gameplay, spawn and wreck lifecycle code must not run in a preview.
        foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
        {
            var part = new GameObject(renderer.name); part.layer = LAYER; part.transform.SetParent(root.transform, false);
            var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            part.transform.localPosition = matrix.GetColumn(3); part.transform.localRotation = matrix.rotation; part.transform.localScale = matrix.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        return root;
    }

    static void Frame(Camera camera, GameObject root, Vector3 direction)
    {
        var bounds = BoundsOf(root);
        camera.transform.position = bounds.center + direction.normalized * 200;
        camera.transform.LookAt(bounds.center);
        var points = root.GetComponentsInChildren<MeshFilter>(true).SelectMany(f => f.sharedMesh.vertices.Select(v => camera.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
        float halfWidth = points.Max(p => Mathf.Abs(p.x)), halfHeight = points.Max(p => Mathf.Abs(p.y));
        camera.orthographicSize = Mathf.Max(halfWidth, halfHeight) * 1.15f;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshFilter>(true).SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds;
    }

    static void Light(Scene scene, string name, float intensity, Vector3 rotation)
    {
        var owner = new GameObject(name); SceneManager.MoveGameObjectToScene(owner, scene);
        var light = owner.AddComponent<UnityEngine.Light>(); light.type = LightType.Directional; light.intensity = intensity; light.cullingMask = 1 << LAYER;
        owner.transform.rotation = Quaternion.Euler(rotation);
    }

    static void Render(Camera camera, RenderTexture target, Texture2D texture, string path)
    {
        camera.Render(); RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
    }
}
