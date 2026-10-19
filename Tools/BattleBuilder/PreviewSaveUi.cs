using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.Saves;

public static class PreviewSaveUi
{
    public static string Run()
    {
        Directory.CreateDirectory("output/BattleSessionSave");
        Render("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab", "Play");
        Render("Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab", "Pause");
        Render("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab", "Load");
        Render("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab", "Name");
        return "Rendered Play, Pause, Load and Name.";
    }

    private sealed class PreviewPresenter : ISaveBrowserPresenter
    {
        public void Close() {}
        public void SelectMode(int index) {}
        public void SelectSave(int index) {}
        public void SaveAsNew() {}
        public void ConfirmName(string name) {}
        public void CancelName() {}
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

                view.SetMenuVisible(true);
            }
            else if (name == "Play") root.GetComponent<PlayUi>().Show();
            else
            {
                var view = root.GetComponent<SaveBrowserUi>();
                view.AssignPresenter(new PreviewPresenter());
                view.SetTitle(name == "Name" ? "SAVE GAME — SKIRMISH" : "LOAD GAME");
                view.SetMode(0, name != "Name");
                view.SetSaveAsNewVisible(name == "Name");
                view.SetSaves(new System.Collections.Generic.List<string> {
                    "Battle above Coruscant — 19/10/2026 17:00 — 00:24:30",
                    "Outer Rim offensive — 18/10/2026 20:45 — 00:42:16",
                    "quicksave — 18/10/2026 20:30 — 00:37:02"
                }, new System.Collections.Generic.List<bool> {true,true,true});
                view.SetStatus("");
                view.Show();
                view.SetNameEntryVisible(name == "Name");
            }            Canvas.ForceUpdateCanvases();
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
