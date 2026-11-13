using System;
using System.IO;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Models.Players;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PreviewMapArchetypeUi
{
    private const string OUTPUT = "Logs/MapArchetypes";
    private const int WIDTH = 1920;
    private const int HEIGHT = 1080;

    public static string Run()
    {
        Directory.CreateDirectory(OUTPUT);
        Render(false);
        Render(true);
        return Path.GetFullPath(OUTPUT) + ": skirmish-ui.png, skirmish-ui-bottom.png, gallery.png, gallery-bottom.png";
    }

    private static void Render(bool gallery)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(WIDTH, HEIGHT, 24);
        Texture2D pixels = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGB24, false);
        SkirmishUi skirmish = null;
        PlanetSelectionPopupUi popup = null;
        try
        {
            var cameraRoot = new GameObject("MapUiPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = HEIGHT * 0.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(16, 26, 41, 255);
            camera.targetTexture = target;

            var canvasRoot = new GameObject("MapUiPreviewCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasRoot.AddComponent<GraphicRaycaster>();

            var model = new SkirmishModel();
            model.SelectMapSize(MapSize.Large);
            for (int i = 2; i < MatchRules.MAX_PLAYERS; i++)
                model.SelectSlotOccupant(i, SkirmishSlotOccupant.AiMedium);
            string path = gallery ? "Assets/Prefabs/Ui/Popups/PlanetSelectionPopupUi.prefab"
                : "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), canvasRoot.transform);
            root.SetActive(true);
            ScrollRect scroll;
            if (gallery)
            {
                popup = RootView<PlanetSelectionPopupUi>(root);
                popup.Initialize(model);
                using var fields = new SerializedObject(popup);
                ((CanvasGroup)fields.FindProperty("canvasGroup").objectReferenceValue).alpha = 1f;
                scroll = (ScrollRect)fields.FindProperty("scrollRect").objectReferenceValue;
            }
            else
            {
                skirmish = RootView<SkirmishUi>(root);
                skirmish.SetModel(model);
                skirmish.AssignPresenter(new PreviewPresenter());
                skirmish.SetData(AssetDatabase.LoadAssetAtPath<TeamColorPalette>(
                    "Assets/Settings/Data/Models/Players/TeamColorPalette.asset"));
                skirmish.Initialize();
                skirmish.Show();
                using var fields = new SerializedObject(skirmish);
                var first = (SkirmishSlotRowView)fields.FindProperty("slotRows").GetArrayElementAtIndex(0).objectReferenceValue;
                scroll = RootView<ScrollRect>(first.transform.parent.parent.gameObject);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            Capture(camera, target, pixels, gallery ? "gallery.png" : "skirmish-ui.png");
            scroll.verticalNormalizedPosition = 0f;
            Capture(camera, target, pixels, gallery ? "gallery-bottom.png" : "skirmish-ui-bottom.png");
        }
        finally
        {
            if (skirmish != null) skirmish.Dispose();
            if (popup != null) popup.Dispose();
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(pixels);
        }
    }

    private static void Capture(Camera camera, RenderTexture target, Texture2D pixels, string filename)
    {
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0f, 0f, WIDTH, HEIGHT), 0, 0);
        pixels.Apply();
        File.WriteAllBytes(Path.Combine(OUTPUT, filename), pixels.EncodeToPNG());
    }

    private static T RootView<T>(GameObject root) where T : Component
    {
        foreach (Component component in root.GetComponents(typeof(Component)))
            if (component is T view) return view;
        throw new InvalidOperationException($"{root.name} has no {typeof(T).Name}.");
    }

    private sealed class PreviewPresenter : ISkirmishPresenter
    {
        public void Close() { }
        public void StartGame() { }
        public void SelectSlotOccupant(int slotIndex, int occupantIndex) { }
        public void SelectSlotFaction(int slotIndex, int factionIndex) { }
        public void SelectSlotTeam(int slotIndex, int teamIndex) { }
        public void SelectSlotColor(int slotIndex, int colorIndex) { }
        public void SelectPlanet(int index) { }
        public void SelectMapSize(int index) { }
        public void SelectVictoryCondition(int index) { }
        public void SelectStartingMoney(float amount) { }
        public void SelectSuperWeapons(bool enabled) { }
    }
}
