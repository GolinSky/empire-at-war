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
        // Edit mode never assigns graphic depth (it stays -1), so a real GraphicRaycaster cannot hit anything here.
        // Stub raycasters exercise UiHitTest's own rule: only UI (GraphicRaycaster) hits block world input.
        [Test]
        public void UiHitTest_BlocksOnlyGraphicRaycasterHits()
        {
            BaseRaycaster[] existing = RaycasterManager.GetRaycasters()
                .Where(raycaster => raycaster.isActiveAndEnabled).ToArray();
            EventSystem previous = EventSystem.current;
            var events = new GameObject("InputTestEvents", typeof(EventSystem));
            var canvas = new GameObject("InputTestCanvas", typeof(RectTransform), typeof(Canvas));
            var world = new GameObject("InputTestWorld");
            StubGraphicRaycaster ui = canvas.AddComponent<StubGraphicRaycaster>();
            StubWorldRaycaster physics = world.AddComponent<StubWorldRaycaster>();
            EventSystem system = events.GetComponent<EventSystem>();
            bool systemEnabled = true;
            try
            {
                foreach (BaseRaycaster raycaster in existing) raycaster.enabled = false;
                // EventSystem and raycasters are not ExecuteAlways, so edit mode never enables them.
                SetEnabledInEditMode(system, true);
                SetEnabledInEditMode(ui, true);
                SetEnabledInEditMode(physics, true);
                EventSystem.current = system;
                var hitTest = new UiHitTest();

                ui.Hit = true;
                Assert.That(hitTest.IsOverUi(Vector2.one), Is.True, "A UI graphic under the pointer blocks the world.");
                physics.Hit = true;
                Assert.That(hitTest.IsOverUi(Vector2.one), Is.True, "UI still blocks when a world collider is also hit.");
                ui.Hit = false;
                Assert.That(hitTest.IsOverUi(Vector2.one), Is.False, "World colliders are not UI.");
                physics.Hit = false;
                Assert.That(hitTest.IsOverUi(Vector2.one), Is.False);

                SetEnabledInEditMode(system, false);
                systemEnabled = false;
                Assert.That(hitTest.IsOverUi(Vector2.one), Is.False, "No event system means no UI.");
            }
            finally
            {
                SetEnabledInEditMode(physics, false);
                SetEnabledInEditMode(ui, false);
                if (systemEnabled) SetEnabledInEditMode(system, false);
                Object.DestroyImmediate(world);
                Object.DestroyImmediate(canvas);
                Object.DestroyImmediate(events);
                foreach (BaseRaycaster raycaster in existing) raycaster.enabled = true;
                // The getter only returns registered systems, so restoring a non-null previous is always valid.
                if (previous != null) EventSystem.current = previous;
            }
        }

        private static void SetEnabledInEditMode(UIBehaviour behaviour, bool enabled) =>
            typeof(UIBehaviour).GetMethod(enabled ? "OnEnable" : "OnDisable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(behaviour, null);

        private sealed class StubGraphicRaycaster : GraphicRaycaster
        {
            public bool Hit { get; set; }

            public override void Raycast(PointerEventData eventData, System.Collections.Generic.List<RaycastResult> resultAppendList)
            {
                if (Hit) resultAppendList.Add(new RaycastResult { module = this, gameObject = gameObject });
            }
        }

        private sealed class StubWorldRaycaster : BaseRaycaster
        {
            public bool Hit { get; set; }
            public override UnityEngine.Camera eventCamera => null;

            public override void Raycast(PointerEventData eventData, System.Collections.Generic.List<RaycastResult> resultAppendList)
            {
                if (Hit) resultAppendList.Add(new RaycastResult { module = this, gameObject = gameObject });
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
                "/ButtonsTemplate/", "/IconTemplate/", "/Popups/SteamInvitePopupUi.prefab" };
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
