#if UNITY_EDITOR
using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityCamera = UnityEngine.Camera;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SliderInteractionTests
    {
        private const string SETTINGS_PATH = "Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab";
        private const string SKIRMISH_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
        private const string SHARED_PATH = "Assets/Prefabs/Ui/Controls/MenuSlider.prefab";
        private const string SKIRMISH_SLIDER_PATH = "Assets/Prefabs/Ui/Controls/SkirmishSlider.prefab";

        [TestCase("MasterVolumeRow", 0.98f)]
        [TestCase("MusicVolumeRow", 0.98f)]
        [TestCase("VoiceVolumeRow", 0.98f)]
        [TestCase("SfxVolumeRow", 0.98f)]
        [TestCase("PanSpeedRow", 2.95f)]
        [TestCase("ZoomSpeedRow", 2.95f)]
        public void SettingsBarClick_MovesHandleAndReportsRoundedValue(string rowName, float expectedValue)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SETTINGS_PATH);
            SettingsSliderRow source = System.Array.Find(
                prefab.GetComponentsInChildren<SettingsSliderRow>(true), row => row.name == rowName);
            GameObject events = new GameObject("Slider test events", typeof(EventSystem), typeof(UnityCamera));
            GameObject canvas = CreateCanvas(events.GetComponent<UnityCamera>());
            SettingsSliderRow row = Object.Instantiate(source, canvas.transform);
            try
            {
                Slider slider = (Slider)new SerializedObject(row).FindProperty("slider").objectReferenceValue;
                row.Initialize();
                float reportedValue = -1f;
                row.ValueChanged += value => { reportedValue = value; row.Render(value); };
                slider.SetValueWithoutNotify(slider.minValue);
                PlaceSlider(slider, canvas.transform);

                ClickBar(slider, events.GetComponent<EventSystem>(), 0.98f);

                Assert.That(reportedValue, Is.EqualTo(expectedValue).Within(0.0001f));
                Assert.That(slider.value, Is.EqualTo(reportedValue).Within(0.0001f));
                Assert.That(slider.handleRect.anchorMin.x, Is.EqualTo(slider.normalizedValue).Within(0.0001f));
                row.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(canvas);
                RenderTexture target = events.GetComponent<UnityCamera>().targetTexture;
                Object.DestroyImmediate(events);
                Object.DestroyImmediate(target);
            }
        }

        [TestCase(0f, 500f)]
        [TestCase(0.5f, 5200f)]
        [TestCase(0.98f, 9800f)]
        [TestCase(1f, 10000f)]
        public void StartingMoneyBarClick_ReportsNearestHundred(float fraction, float expectedValue)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SKIRMISH_PATH);
            Slider source = (Slider)new SerializedObject(prefab.GetComponent<SkirmishUi>())
                .FindProperty("startingMoneySlider").objectReferenceValue;
            GameObject events = new GameObject("Slider test events", typeof(EventSystem), typeof(UnityCamera));
            GameObject canvas = CreateCanvas(events.GetComponent<UnityCamera>());
            Slider slider = Object.Instantiate(source, canvas.transform);
            try
            {
                SkirmishModel model = new SkirmishModel();
                slider.onValueChanged.AddListener(model.SelectStartingMoney);
                model.Changed += () => slider.SetValueWithoutNotify(model.StartingMoney);
                slider.SetValueWithoutNotify(fraction == 0f ? slider.maxValue : slider.minValue);
                PlaceSlider(slider, canvas.transform);

                ClickBar(slider, events.GetComponent<EventSystem>(), fraction);

                Assert.That(model.StartingMoney, Is.EqualTo(expectedValue));
                Assert.That(slider.value, Is.EqualTo(expectedValue));
                Assert.That(slider.handleRect.anchorMin.x, Is.EqualTo(slider.normalizedValue).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
                RenderTexture target = events.GetComponent<UnityCamera>().targetTexture;
                Object.DestroyImmediate(events);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void MatchingSliders_UseSharedPrefabAndSizeVariant()
        {
            GameObject shared = AssetDatabase.LoadAssetAtPath<GameObject>(SHARED_PATH);
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(SKIRMISH_SLIDER_PATH);
            Assert.That(shared, Is.Not.Null);
            Assert.That(variant, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(variant), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.EqualTo(shared));

            foreach (Slider slider in AssetDatabase.LoadAssetAtPath<GameObject>(SETTINGS_PATH)
                         .GetComponentsInChildren<Slider>(true))
            {
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(slider), Is.EqualTo(SHARED_PATH));
            }

            Slider money = AssetDatabase.LoadAssetAtPath<GameObject>(SKIRMISH_PATH).GetComponentInChildren<Slider>(true);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(money), Is.EqualTo(SKIRMISH_SLIDER_PATH));
            Assert.That(((RectTransform)shared.transform).sizeDelta, Is.EqualTo(new Vector2(470f, 28f)));
            Assert.That(((RectTransform)variant.transform).sizeDelta, Is.EqualTo(new Vector2(448f, 28f)));
        }

        private static GameObject CreateCanvas(UnityCamera camera)
        {
            GameObject canvas = new GameObject("Slider test canvas", typeof(Canvas), typeof(GraphicRaycaster));
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 100f;
            camera.targetTexture = new RenderTexture(800, 200, 24);
            Canvas view = canvas.GetComponent<Canvas>();
            view.renderMode = RenderMode.ScreenSpaceCamera;
            view.worldCamera = camera;
            view.planeDistance = 1f;
            return canvas;
        }

        private static void PlaceSlider(Slider slider, Transform canvas)
        {
            RectTransform rect = (RectTransform)slider.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            slider.GetComponentInParent<Canvas>().worldCamera.Render();
        }

        private static void ClickBar(Slider slider, EventSystem events, float fraction)
        {
            RectTransform track = (RectTransform)slider.handleRect.parent;
            Vector3 position = track.TransformPoint(new Vector3(
                Mathf.Lerp(track.rect.xMin, track.rect.xMax, fraction), track.rect.center.y, 0f));
            position.y = slider.transform.TransformPoint(new Vector3(0f, 8f, 0f)).y;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(events.GetComponent<UnityCamera>(), position);
            PointerEventData pointer = new PointerEventData(events)
            {
                position = screenPoint,
                button = PointerEventData.InputButton.Left
            };
            List<RaycastResult> hits = new List<RaycastResult>();
            // EditMode does not register raycasters through OnEnable; use the canvas raycaster directly.
            slider.GetComponentInParent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "The full slider bar must accept clicks above the thin visible track.");
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            GameObject handler = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(handler, Is.EqualTo(slider.gameObject));
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerUpHandler);
        }
    }
}
#endif
