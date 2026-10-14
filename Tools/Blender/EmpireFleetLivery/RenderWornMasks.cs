using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderWornMasks
{
    public static string Main(string ship, bool wreck = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Render in Edit Mode.");
        string faction = ship == "Imperator" ? "Republic" : "Empire";
        string folder = "Tools/Blender/EmpireFleetLivery/Previews/WornMasks/" + ship + (wreck ? "/Wreck" : "");
        Directory.CreateDirectory(folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var palette = Shader.GetGlobalVectorArray("_TeamColors");
        var ambient = RenderSettings.ambientLight;
        var mode = RenderSettings.ambientMode;
        var active = RenderTexture.active;
        string prefabPath = wreck ? "Assets/Prefabs/Models/Wrecks/" + ship + "WreckView.prefab" : "Assets/Prefabs/Models/Ships/" + faction + "/" + ship + ".prefab";
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        float size = Mathf.Max(bounds.size.x, bounds.size.z);
        var copies = new System.Collections.Generic.List<Material>();
        if (wreck)
        {
            foreach (var renderer in renderers)
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                {
                    var copy = new Material(original); copies.Add(copy);
                    copy.SetVector("_WreckAxis", Vector3.forward); copy.SetVector("_WreckCenter", bounds.center);
                    copy.SetVector("_WreckCuts", new Vector4(-size * .1f, size * .14f, 1000000, 1000000));
                    copy.SetVector("_WreckAxisRange", new Vector4(-size, size, 0, 0));
                    copy.SetFloat("_WreckStartTime", 0); copy.SetFloat("_WreckLifetime", 1000000000);
                    copy.SetFloat("_WreckGlowDuration", 1000000000); copy.SetFloat("_WreckSeparationSpeed", 0);
                    copy.SetFloat("_WreckTiltSpeed", 0); copy.SetFloat("_WreckSinkSpeed", 0);
                    return copy;
                }).ToArray();
        }
        var cameraObject = new GameObject("MaskPreviewCamera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene; camera.enabled = false; camera.cullingMask = 1 << 30;
        camera.orthographic = true; camera.orthographicSize = size * .6f;
        camera.nearClipPlane = .01f; camera.farClipPlane = size * 10;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .04f, .055f, 1);
        camera.transform.position = bounds.center + new Vector3(1.1f, 1.6f, 1.2f) * size;
        camera.transform.LookAt(bounds.center);
        foreach (var row in new[] { (new Vector3(35, 30, 0), 1.6f), (new Vector3(-20, -130, 0), .7f) })
        {
            var go = new GameObject("MaskPreviewLight");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = row.Item2;
            light.cullingMask = 1 << 30; light.transform.rotation = Quaternion.Euler(row.Item1);
        }
        var target = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        var texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .42f, .47f);
            Shader.SetGlobalVectorArray("_TeamColors", new[] { new Vector4(.2f, .55f, 1, 1), new Vector4(1, .25f, .2f, 1), new Vector4(.3f, .9f, .4f, 1), Vector4.one, Vector4.one, Vector4.one, Vector4.one, Vector4.one });
            for (int team = 0; team <= 3; team++)
            {
                foreach (var renderer in renderers) renderer.SetShaderUserValue((uint)team);
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); texture.Apply();
                File.WriteAllBytes(folder + "/Team" + team + ".png", texture.EncodeToPNG());
            }
        }
        finally
        {
            RenderTexture.active = active; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var copy in copies) UnityEngine.Object.DestroyImmediate(copy);
            RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = mode;
            Shader.SetGlobalVectorArray("_TeamColors", palette.Length > 0 ? palette : new Vector4[8]);
        }
        return ship + ": saved neutral, blue, red and green previews.";
    }
}
