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
    private const string WHITE_ICONS = "Assets/ThirdParty/SCI-FI UI Pack Pro/Common/White Icons/Textures/Icons1/";
    private const int PREVIEW_LAYER = 31;

    public static string Bind()
    {
        var data = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH);
        var serialized = new SerializedObject(data);
        SerializedProperty artwork = serialized.FindProperty("structureStatusIcons");
        string[] fields = { "mining", "satellite", "relay", "captured", "built", "destroyed" };
        string[] paths = {
            "Assets/Art/Textures/Ui/Icons/MiniMap/MiningFacilityWhite.png",
            "Assets/Art/Textures/Ui/Icons/MiniMap/DefendPlatformWhite.png",
            WHITE_ICONS + "ICON_Wifi.png",
            "Assets/Art/Textures/Ui/TacticalMarkers/Capture.png",
            WHITE_ICONS + "Icon_Right.png",
            WHITE_ICONS + "Icon_Wrong.png"
        };
        for (int i = 0; i < fields.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprite == null) throw new InvalidOperationException("Missing sprite: " + paths[i]);
            artwork.FindPropertyRelative(fields[i]).objectReferenceValue = sprite;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return "Saved six shared structure-status sprite bindings.";
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
            Label(root, "Shared artwork in world and minimap • actual Unity UI render", -470, 192, 950, 16);
            string[] rows = { "Mining / Battle Asteroid", "Satellite pad", "Relay" };
            string[] columns = { "CAPTURED", "BUILT", "DESTROYED" };
            for (int c = 0; c < 3; c++) Label(root, columns[c], -130 + c * 220, 135, 200, 17, FontStyle.Bold);
            for (int r = 0; r < 3; r++)
            {
                float y = 65 - r * 115;
                Label(root, rows[r], -470, y, 320, 18);
                for (int c = 0; c < 3; c++)
                {
                    if (r == 2 && c > 0) continue;
                    float x = -80 + c * 220;
                    var kind = (StructureStatusIconKind)r;
                    var state = (StructureStatusIconState)c;
                    StructureStatusIconView world = StructureStatusIconView.Create(root, 64f, PREVIEW_LAYER);
                    world.SetAppearance(artwork, kind, state);
                    world.Display(true, 1f, new Vector2(x, y));
                    StructureStatusIconView mini = StructureStatusIconView.Create(root, 26f, PREVIEW_LAYER);
                    mini.SetAppearance(artwork, kind, state);
                    mini.Display(true, 1f, new Vector2(x + 70f, y - 12f));
                }
            }
            Label(root, "Enlarged world artwork + 26 px minimap sample. Relay has capture only.", -470, -235, 950, 16);
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
