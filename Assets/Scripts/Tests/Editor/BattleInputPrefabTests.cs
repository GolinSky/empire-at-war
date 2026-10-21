using System.Linq;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class BattleInputPrefabTests
    {
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
