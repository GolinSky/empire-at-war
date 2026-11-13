using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RenderConstructionPod
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = RenderTexture.GetTemporary(768, 768, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(768, 768, TextureFormat.RGBA32, false);
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Models/SpaceStations/ConstructionPod.prefab"), scene);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var cameraObject = new GameObject("ConstructionPodPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .1f, .14f, 1);
            camera.orthographic = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 5000;
            camera.targetTexture = target;
            foreach (var rotation in new[] {new Vector3(35, 30, 0), new Vector3(-20, -130, 0)})
            {
                var lightObject = new GameObject("ConstructionPodPreviewLight");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.cullingMask = 1 << 30;
                lightObject.transform.rotation = Quaternion.Euler(rotation);
            }
            int view = 0;
            foreach (var direction in new[] {new Vector3(1, .7f, 1), new Vector3(-1, .5f, -1)})
            {
                camera.transform.position = bounds.center + direction.normalized * 1800;
                camera.transform.LookAt(bounds.center);
                var points = renderers.SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(
                    v => camera.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
                camera.orthographicSize = points.Max(p => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y))) * 1.15f;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                image.Apply();
                File.WriteAllBytes("Temp/ConstructionPodImport/Preview" + view++ + ".png", image.EncodeToPNG());
            }
            return "Saved two isolated Unity preview renders.";
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            RenderTexture.ReleaseTemporary(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
