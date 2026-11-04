using System.Linq;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Services.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class BattleInputPrefabTests
    {
        [TestCase(0)]
        [TestCase(5)]
        public void UiHitTest_BlocksGraphicsOnAnyLayerAndRespectsRaycastVisibility(int layer)
        {
            BaseRaycaster[] existing = RaycasterManager.GetRaycasters()
                .Where(raycaster => raycaster.isActiveAndEnabled).ToArray();
            EventSystem previous = EventSystem.current;
            var events = new GameObject("InputTestEvents", typeof(EventSystem));
            var canvasRoot = new GameObject("InputTestCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            try
            {
                foreach (BaseRaycaster raycaster in existing) raycaster.enabled = false;
                EventSystem.current = events.GetComponent<EventSystem>();
                canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var control = new GameObject("Control", typeof(RectTransform), typeof(Image));
                control.layer = layer;
                control.transform.SetParent(canvasRoot.transform, false);
                Image graphic = control.GetComponent<Image>();
                graphic.rectTransform.sizeDelta = new Vector2(100f, 100f);
                Canvas.ForceUpdateCanvases();
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, graphic.rectTransform.position);
                var hitTest = new UiHitTest();
                Assert.That(hitTest.IsOverUi(point), Is.True);

                CanvasGroup group = canvasRoot.GetComponent<CanvasGroup>();
                group.interactable = false;
                Assert.That(hitTest.IsOverUi(point), Is.True, "Disabled UI still blocks world clicks.");
                group.blocksRaycasts = false;
                Assert.That(hitTest.IsOverUi(point), Is.False, "Hidden UI must not block the world.");
                group.blocksRaycasts = true;
                graphic.raycastTarget = false;
                Assert.That(hitTest.IsOverUi(point), Is.False, "Decorative graphics must remain transparent to input.");
            }
            finally
            {
                Object.DestroyImmediate(canvasRoot);
                Object.DestroyImmediate(events);
                foreach (BaseRaycaster raycaster in existing) raycaster.enabled = true;
                EventSystem.current = previous;
            }
        }

        [TestCase("Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab")]
        [TestCase("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab")]
        public void Modal_HasFullScreenRaycastBlockerBehindPanel(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            RectTransform rootRect = (RectTransform)root.transform;
            Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rootRect.sizeDelta, Is.EqualTo(Vector2.zero));

            Transform blocker = root.transform.GetChild(0);
            Assert.That(blocker.name, Is.EqualTo("InputBlocker"));
            Assert.That(blocker.gameObject.activeSelf, Is.True);
            RectTransform rect = (RectTransform)blocker;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            Image image = blocker.GetComponent<Image>();
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(image.color.a, Is.Zero);
            Assert.That(blocker.GetComponent<CanvasGroup>(), Is.Null);

            var view = new SerializedObject(root.GetComponent<BaseUi>());
            Assert.That(view.FindProperty("canvasGroup").objectReferenceValue,
                Is.EqualTo(root.GetComponent<CanvasGroup>()),
                "The blocker must share the view's Show/Hide raycast state.");
        }

        [Test]
        public void SaveBrowser_RetainsKeyboardNavigation()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab");
            Assert.That(root.GetComponentsInChildren<Button>(true)
                .Any(button => button.navigation.mode != Navigation.Mode.None), Is.True);
        }

        [Test]
        public void BattleHudControls_DoNotAcquireKeyboardFocusOrNavigate()
        {
            string[] excluded = { "/MainMenu/", "/SkirmishMenu/", "/Controls/",
                "/ButtonsTemplate/", "/IconTemplate/" };
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Ui" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !excluded.Any(path.Contains) && !path.EndsWith("/EndGameUi.prefab"))
                .ToArray();
            int controls = 0;
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Selectable selectable in prefab.GetComponentsInChildren<Selectable>(true))
                {
                    Assert.That(selectable.navigation.mode, Is.EqualTo(Navigation.Mode.None),
                        $"{path}: {selectable.name} can steal camera keys or receive Submit after a click.");
                    controls++;
                }
            }
            Assert.That(controls, Is.GreaterThan(0));
        }
    }
}
