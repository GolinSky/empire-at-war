using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderImperialVenatorEngines
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialVenator.prefab"), scene);
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
        var cameraObject = new GameObject("EngineCamera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .035f, .055f);
        camera.cullingMask = 1 << 30; camera.orthographic = true; camera.orthographicSize = 15;
        camera.nearClipPlane = .01f; camera.farClipPlane = 500;
        var lightObject = new GameObject("EngineLight"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 30;
        light.transform.rotation = Quaternion.Euler(20, 180, 0);
        var target = RenderTexture.GetTemporary(1024, 512, 24); camera.targetTexture = target;
        var texture = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            var effects = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.name.Contains("Glow")).ToArray();
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) foreach (var renderer in effects) renderer.enabled = false;
                foreach (var angle in new [] {"Rear", "RearUpper", "RearLower"})
                {
                    camera.transform.position = new Vector3(angle == "Rear" ? 0 : 22, angle == "RearUpper" ? 18 : angle == "RearLower" ? -27 : -7, -120);
                    camera.transform.LookAt(new Vector3(0, -7, -48));
                    camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1024, 512), 0, 0); texture.Apply();
                    File.WriteAllBytes("output/ImperialVenator/Previews/Engine" + angle + (pass == 0 ? "" : "Opaque") + ".png", texture.EncodeToPNG());
                }
            }
        }
        finally
        {
            RenderTexture.active = previousTarget; camera.targetTexture = null; RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture); EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientMode = previousMode; RenderSettings.ambientLight = previousAmbient;
        }
        return "Saved rear/upper/lower engine views with glow and with opaque source geometry only.";
    }
}
