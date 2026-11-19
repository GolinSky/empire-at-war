using System;
using System.IO;
using EmpireAtWar.Entities.StructureStatusIcons;
using EmpireAtWar.Models.MiniMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StructureStatusIconAssets
{
    private const string DATA_PATH = "Assets/Settings/Data/Models/MiniMap/MiniMapData.asset";
    private const string ICON_FOLDER = "Assets/Art/Textures/Ui/Icons/MiniMap/";
    private const int PREVIEW_LAYER = 31;

    public static string Bind()
    {
        var data = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH);
        var serialized = new SerializedObject(data);
        SerializedProperty artwork = serialized.FindProperty("structureStatusIcons");
        (string Field, string FileName)[] bindings = {
            ("captured", "StructureCaptured.png"),
            ("destroyed", "StructureDestroyed.png"),
            ("buildAvailable", "StructureBuildAvailable.png")
        };
        foreach ((string field, string fileName) in bindings)
        {
            string path = ICON_FOLDER + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Missing sprite: " + path);
            artwork.FindPropertyRelative(field).objectReferenceValue = sprite;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return "Saved three shared structure status sprite bindings.";
    }

    public static string Preview()
    {
        var data = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH);
        StructureStatusIconArtwork artwork = data.StructureStatusIcons;
        Scene scene = EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1040, 560, 24, RenderTextureFormat.ARGB32);
        Texture2D readback = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var cameraObject = new GameObject("IconPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            cameraObject.transform.position = new Vector3(0f, 0f, -100f);
            camera.orthographic = true;
            camera.orthographicSize = 280f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(16, 26, 41, 255);
            camera.cullingMask = 1 << PREVIEW_LAYER;
            camera.targetTexture = texture;
            var canvasObject = new GameObject("IconPreviewCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.layer = PREVIEW_LAYER;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var root = (RectTransform)canvasObject.transform;
            root.sizeDelta = new Vector2(1040, 560);
            Label(root, "STRUCTURE STATUS ICONS", -470, 230, 700, 28, FontStyle.Bold);
            Label(root, "Artwork rendered by the Unity UI", -470, 192, 950, 16);
            Label(root, "Battle HUD · 48 px", -470, 45, 260, 18);
            Label(root, "Minimap · 26 px", -470, -65, 260, 18);
            (string Label, StructureStatusIconState State)[] columns = {
                ("CAPTURED", StructureStatusIconState.Captured),
                ("DESTROYED", StructureStatusIconState.Destroyed),
                ("BUILD AVAILABLE", StructureStatusIconState.BuildAvailable)
            };
            for (int column = 0; column < columns.Length; column++)
            {
                (string label, StructureStatusIconState state) = columns[column];
                float x = -100 + column * 220;
                Label(root, label, x - 45, 125, 200, 17, FontStyle.Bold);
                StructureStatusIconView world = StructureStatusIconView.Create(root, 48f, PREVIEW_LAYER);
                world.SetAppearance(artwork, state);
                world.Display(true, 1f, new Vector2(x, 45));
                StructureStatusIconView minimap = StructureStatusIconView.Create(root, 26f, PREVIEW_LAYER);
                minimap.SetAppearance(artwork, state);
                minimap.Display(true, 1f, new Vector2(x, -65));
            }
            Label(root, "The Build Available reminder appears in the battle HUD.", -470, -165, 950, 16);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = texture;
            readback = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readback.Apply();
            string output = Path.GetFullPath("Logs/StructureStatusIcons/structure-status-icons.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, readback.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active = previous;
            if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
            EditorSceneManager.ClosePreviewScene(scene);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void Label(RectTransform parent, string value, float x, float y, float width, int size,
        FontStyle style = FontStyle.Normal)
    {
        var child = new GameObject("Label", typeof(RectTransform));
        child.layer = PREVIEW_LAYER;
        child.transform.SetParent(parent, false);
        Text text = child.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = new Color32(242, 240, 230, 255);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.rectTransform.pivot = new Vector2(0, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(width, 38);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
    }
}
