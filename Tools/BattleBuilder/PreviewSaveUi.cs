using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MainMenu.Play;

public static class PreviewSaveUi
{
    public static string Run()
    {
        Directory.CreateDirectory("output/BattleSessionSave");
        Render("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab", "Play");
        Render("Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab", "Pause");
        return "Rendered Play and Pause in output/BattleSessionSave.";
    }

    private static void Render(string path, string name)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1920, 1080, 24);
        var old = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            var cameraRoot = new GameObject("PreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            var camera = cameraRoot.AddComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 12, 20, 255);
            camera.targetTexture = texture;
            var canvasRoot = new GameObject("PreviewCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), canvasRoot.transform);
            if (name == "Pause")
            {
                var view = root.GetComponent<PauseMenuUi>();
                view.SetSavingAvailable(true);
                view.SetSaveStatus("Saved Battle above Coruscant.");
                view.SetMenuVisible(true);
            }
            else
            {
                var view = root.GetComponent<PlayUi>();
                view.SetSaves(0, new System.Collections.Generic.List<string> { "Battle above Coruscant — 19/10/2026 17:00 — 00:24:30" });
                view.SetLoadState(true, "");
                view.Show();
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = texture;
            pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("output/BattleSessionSave/" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = old;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.ClosePreviewScene(scene);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
