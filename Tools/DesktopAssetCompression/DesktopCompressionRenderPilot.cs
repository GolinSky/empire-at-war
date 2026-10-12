using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class DesktopCompressionRenderPilot
{
    public static string Main(string phase)
    {
        string folder = "output/DesktopAssetCompression/" + phase;
        Directory.CreateDirectory(folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var target = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/AcclamatorAssault.prefab"), scene);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            var cameraObject = new GameObject("CompressionPilotCamera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.cullingMask = 1 << 30;
            camera.orthographic = true;
            camera.orthographicSize = 43;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000;
            camera.targetTexture = target;
            var lightObject = new GameObject("CompressionPilotLight");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.cullingMask = 1 << 30;
            light.transform.rotation = Quaternion.Euler(35, 30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f);
            Color[] colors = { Color.blue, Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.gray };
            Shader.SetGlobalVectorArray("_TeamColors", colors.Select(c => (Vector4)c).ToArray());
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int team = 0; team < colors.Length; team++)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)(team + 1));
                for (int view = 0; view < 2; view++)
                {
                    camera.transform.position = view == 0 ? new Vector3(50, 60, 70) : new Vector3(0, 200, 0);
                    camera.transform.LookAt(Vector3.zero);
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/Team" + team + "-View" + view + ".png", image.EncodeToPNG());
                }
            }
        }
        finally
        {
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousAmbient;
            if (previousPalette.Length > 0) Shader.SetGlobalVectorArray("_TeamColors", previousPalette);
        }
        return folder;
    }
}
