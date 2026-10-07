using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RenderRebelStations
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var previousPalette = Shader.GetGlobalVectorArray("_TeamColors");
        var previousAmbient = RenderSettings.ambientLight;
        var previousMode = RenderSettings.ambientMode;
        var previousTarget = RenderTexture.active;
        var cameraObject = new GameObject("StationPreviewCamera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene; camera.enabled = false; camera.orthographic = true;
        camera.orthographicSize = 240; camera.nearClipPlane = .1f; camera.farClipPlane = 3000;
        camera.cullingMask = 1 << 30;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .035f, .055f, 1);
        camera.transform.position = new Vector3(500, 400, 600); camera.transform.LookAt(Vector3.zero);
        var lightObject = new GameObject("StationPreviewLight");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.8f; light.cullingMask = 1 << 30;
        light.transform.rotation = Quaternion.Euler(35, 30, 0);
        var target = RenderTexture.GetTemporary(700, 700, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(700, 700, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        Directory.CreateDirectory("Temp/RebelStationImport/Previews");
        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .52f, .57f);
            Color[] colors = {new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};
            Shader.SetGlobalVectorArray("_TeamColors", colors.Select(c => (Vector4)c).ToArray());
            for (int level = 1; level <= 5; level++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Stations/RebelSpaceStationLevel" + level + ".prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                for (int team = 0; team < 8; team++)
                {
                    foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue((uint)(team + 1));
                    camera.Render(); RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); texture.Apply();
                    File.WriteAllBytes("Temp/RebelStationImport/Previews/Level" + level + "Team" + team + ".png", texture.EncodeToPNG());
                }
                // Frame the common dome independently of each level's centered overall bounds.
                var focus = root.transform.Find("RebelSpaceStationLevel" + level).TransformPoint(new Vector3(0, 4.46f, 0));
                camera.orthographicSize = 85;
                camera.transform.position = focus + new Vector3(100, 540, -600); camera.transform.LookAt(focus);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.SetShaderUserValue(1);
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); texture.Apply();
                File.WriteAllBytes("Temp/RebelStationImport/Previews/Level" + level + "Closeup.png", texture.EncodeToPNG());
                camera.orthographicSize = 240;
                camera.transform.position = new Vector3(500, 400, 600); camera.transform.LookAt(Vector3.zero);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        finally
        {
            RenderTexture.active = previousTarget; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
            RenderSettings.ambientLight = previousAmbient; RenderSettings.ambientMode = previousMode;
            if (previousPalette.Length > 0) Shader.SetGlobalVectorArray("_TeamColors", previousPalette);
        }
        return "Rendered five station levels in all eight team palettes and five dome close-ups.";
    }
}
