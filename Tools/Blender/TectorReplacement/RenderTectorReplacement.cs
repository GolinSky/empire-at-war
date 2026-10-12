using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderTectorReplacement
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/";
    private const string ICONS = "Assets/Art/Textures/Ui/Icons/ShipIcon/";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tector rendering requires Edit Mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var target = RenderTexture.GetTemporary(768, 768, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(768, 768, TextureFormat.RGBA32, false);
        var temporaryMaterials = new List<Material>();
        try
        {
            Directory.CreateDirectory(OUTPUT + "Previews");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/Tector.prefab"), scene);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            var cameraObject = new GameObject("TectorPreviewCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 30;
            camera.orthographic = true; camera.orthographicSize = 108; camera.nearClipPlane = .01f; camera.farClipPlane = 1000;
            camera.targetTexture = target;
            foreach (var row in new[] { ("Key", new Vector3(35, 30, 0), 1.6f), ("Fill", new Vector3(-20, -130, 0), .7f) })
            {
                var lightObject = new GameObject(row.Item1); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = row.Item3;
                light.cullingMask = 1 << 30; light.transform.rotation = Quaternion.Euler(row.Item2);
            }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .42f, .47f);
            camera.transform.position = new Vector3(240, 280, 330); camera.transform.LookAt(Vector3.zero);
            Render(camera, target, texture, OUTPUT + "Previews/Angled.png");
            var icon = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            try
            {
                var iconTarget = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
                try
                {
                    camera.targetTexture = iconTarget;
                    Render(camera, iconTarget, icon, ICONS + "TectorIcon.png");
                    var pixels = icon.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, pixels[i].a);
                    icon.SetPixels32(pixels); icon.Apply(); File.WriteAllBytes(ICONS + "TectorSilhouette.png", icon.EncodeToPNG());
                }
                finally { camera.targetTexture = target; RenderTexture.ReleaseTemporary(iconTarget); }
            }
            finally { UnityEngine.Object.DestroyImmediate(icon); }
            camera.transform.position = new Vector3(0, 500, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            Render(camera, target, texture, OUTPUT + "Previews/Top.png");
            camera.transform.position = new Vector3(220, -230, 300); camera.transform.LookAt(Vector3.zero);
            Render(camera, target, texture, OUTPUT + "Previews/Underside.png");
            camera.transform.position = new Vector3(0, 80, -330); camera.transform.LookAt(Vector3.zero);
            Render(camera, target, texture, OUTPUT + "Previews/Rear.png");
            camera.transform.position = new Vector3(240, 280, 330); camera.transform.LookAt(Vector3.zero);
            Color[] colors = { new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f) };
            Shader.SetGlobalVectorArray("_TeamColors", colors.Select(c => (Vector4)c).ToArray());
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            for (int i = 0; i < 8; i++) { foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1)); Render(camera,target,texture,OUTPUT + "Previews/Team" + i + ".png"); }
            root.SetActive(false);
            var placement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab"),scene);
            foreach (var transform in placement.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            Render(camera,target,texture,OUTPUT + "Previews/Placement.png"); placement.SetActive(false);
            var wreck = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab"),scene);
            foreach (var transform in wreck.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            renderers = wreck.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
            {
                var material = new Material(source); temporaryMaterials.Add(material);
                material.SetFloat("_WreckLifetime", 1000000); material.SetFloat("_WreckSeparationSpeed", 0);
                material.SetFloat("_WreckTiltSpeed", 0); material.SetFloat("_WreckSinkSpeed", 0); material.SetVector("_WreckCuts", Vector4.one * 1000000);
                return material;
            }).ToArray();
            for (int i = 0; i < 8; i++) { foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(i + 1)); Render(camera,target,texture,OUTPUT + "Previews/WreckTeam" + i + ".png"); }
        }
        finally
        {
            RenderTexture.active = previousTarget; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in temporaryMaterials) UnityEngine.Object.DestroyImmediate(material);
            RenderSettings.ambientMode = previousMode; RenderSettings.ambientLight = previousAmbient;
            Shader.SetGlobalVectorArray("_TeamColors", previousPalette.Length > 0 ? previousPalette : new Vector4[8]);
        }
        AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
        return "Rendered actual Tector replacement, transparent 512px icon/silhouette, placement, and eight live/wreck palettes.";
    }
    private static void Render(Camera camera, RenderTexture target, Texture2D texture, string path)
    {
        camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }
}
