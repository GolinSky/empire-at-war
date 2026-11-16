using System.IO;
using EmpireAtWar.Entities.ControlGroups;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PreviewControlGroups
{
    private const string OUTPUT = "Logs/ControlGroups";
    private const int WIDTH = 1920;
    private const int HEIGHT = 1080;

    public static string Run()
    {
        Directory.CreateDirectory(OUTPUT);
        Render(false);
        Render(true);
        return Path.GetFullPath(OUTPUT);
    }

    private static void Render(bool settings)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(WIDTH, HEIGHT, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            var cameraRoot = new GameObject("SquadPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = HEIGHT * 0.5f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(12, 20, 32, 255);
            camera.targetTexture = target;
            var canvasRoot = new GameObject("SquadPreviewCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            if (settings)
            {
                SettingsUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SettingsUi>(
                    "Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab"), canvasRoot.transform);
                var fields = new SerializedObject(view);
                var row = (SettingsDropdownRow)fields.FindProperty("squadModifierRow").objectReferenceValue;
                for (Transform t = row.transform; t != canvasRoot.transform; t = t.parent) t.gameObject.SetActive(true);
                foreach (SettingsCategoryTab tab in view.GetComponentsInChildren<SettingsCategoryTab>(true))
                {
                    var tabFields = new SerializedObject(tab);
                    var panel = (CanvasGroup)tabFields.FindProperty("panel").objectReferenceValue;
                    var toggle = (Toggle)tabFields.FindProperty("categoryToggle").objectReferenceValue;
                    toggle.isOn = row.transform.IsChildOf(panel.transform);
                    panel.alpha = toggle.isOn ? 1f : 0f;
                }
                view.Show();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row.transform.parent);
                Canvas.ForceUpdateCanvases();
                foreach (ScrollRect scroll in view.GetComponentsInChildren<ScrollRect>(true))
                    if (row.transform.IsChildOf(scroll.content)) scroll.verticalNormalizedPosition = 0;
            }
            else
            {
                ControlGroupUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ControlGroupUi>(
                    "Assets/Prefabs/Ui/ControlGroups/ControlGroupUi.prefab"), canvasRoot.transform);
                ShipUiData icons = AssetDatabase.LoadAssetAtPath<ShipUiData>(
                    "Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
                view.Show();
                for (int slot = 0; slot < 10; slot++) view.RenderSlot(slot, 0, null, null, false);
                view.RenderSlot(1, 12, icons.GetShipIcon(ShipType.Venator), icons.GetShipIcon(ShipType.Acclamator), false);
                view.RenderSlot(3, 5, icons.GetShipIcon(ShipType.ISDII), null, true);
                view.RenderSlot(0, 7, icons.GetShipIcon(ShipType.Executor), null, false);
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, WIDTH, HEIGHT), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(OUTPUT, settings ? "settings-modifier.png" : "squad-preview.png"), pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) Object.DestroyImmediate(pixels);
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(target);
        }
    }
}
