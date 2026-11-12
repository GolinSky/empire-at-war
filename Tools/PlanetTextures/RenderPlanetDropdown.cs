using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RenderPlanetDropdown
{
    private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
    private const string OUTPUT_PATH = "Temp/PlanetImport/Renders/PlanetDropdown.png";
    private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Main()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OUTPUT_PATH));
        var scene = EditorSceneManager.NewPreviewScene();
        var previousTarget = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        TMP_Dropdown dropdown = null;
        try
        {
            var cameraObject = new GameObject("PlanetDropdownPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 12, 20, 255);
            camera.targetTexture = target;

            var canvasObject = new GameObject("PlanetDropdownPreviewCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            using (var view = new SerializedObject(prefab.GetComponent<SkirmishUi>()))
            {
                var source = (TMP_Dropdown)view.FindProperty("planetsDropdown").objectReferenceValue;
                dropdown = Object.Instantiate(source, canvasObject.transform, false);
            }
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            var rect = (RectTransform)dropdown.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -150);
            rect.sizeDelta = new Vector2(600, 56);
            rect.localScale = Vector3.one;
            dropdown.gameObject.SetActive(true);
            dropdown.interactable = true;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(Enum.GetNames(typeof(PlanetType))));
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
            dropdown.alphaFadeSpeed = 0;
            // Execute the TMP initialization normally supplied by the Play Mode lifecycle.
            typeof(TMP_Dropdown).GetMethod("Start", PRIVATE_INSTANCE).Invoke(dropdown, null);
            Canvas.ForceUpdateCanvases();
            dropdown.Show();
            var list = (GameObject)typeof(TMP_Dropdown).GetField("m_Dropdown", PRIVATE_INSTANCE).GetValue(dropdown);
            list.GetComponent<CanvasGroup>().alpha = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(OUTPUT_PATH, image.EncodeToPNG());
            return $"{Path.GetFullPath(OUTPUT_PATH)}; template={dropdown.template.rect.height}; options={dropdown.options.Count}";
        }
        finally
        {
            if (dropdown != null)
            {
                dropdown.StopAllCoroutines();
                // TMP uses deferred Destroy in OnDisable; the preview scene owns and immediately
                // disposes these temporary objects instead, without touching any live scene.
                typeof(TMP_Dropdown).GetField("m_Dropdown", PRIVATE_INSTANCE).SetValue(dropdown, null);
                typeof(TMP_Dropdown).GetField("m_Blocker", PRIVATE_INSTANCE).SetValue(dropdown, null);
                ((IList)typeof(TMP_Dropdown).GetField("m_Items", PRIVATE_INSTANCE).GetValue(dropdown)).Clear();
            }
            RenderTexture.active = previousTarget;
            EditorSceneManager.ClosePreviewScene(scene);
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
        }
    }
}
