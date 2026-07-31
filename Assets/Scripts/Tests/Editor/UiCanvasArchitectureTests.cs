using EmpireAtWar.Ui.Base;
using EmpireAtWar.Ui.Popups;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Reinforcement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UiCanvasArchitectureTests
    {
        private const string UI_PREFAB_FOLDER = "Assets/Prefabs/Ui";
        private const string UI_SERVICE_PREFAB_PATH = "Assets/Prefabs/Ui/UiService.prefab";
        private const string ECONOMY_PREFAB_PATH =
            "Assets/Prefabs/Ui/Economy/EconomyUi.prefab";
        private const string REINFORCEMENT_PREFAB_PATH =
            "Assets/Prefabs/Ui/Reinforcement/ReinforcementUi.prefab";
        private const string SHIP_BUILD_PREFAB_PATH =
            "Assets/Prefabs/Ui/Factions/ShipBuildUi.prefab";
        private const int EXPECTED_SCREEN_PREFAB_COUNT = 11;

        [Test]
        public void UiScreenPrefabs_UseBoundCanvasGroupsWithoutLocalCanvases()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { UI_PREFAB_FOLDER });
            int screenCount = 0;

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                BaseUi baseUi = root.GetComponent<BaseUi>();
                PopupUi popupUi = root.GetComponent<PopupUi>();
                MonoBehaviour screen = baseUi != null ? baseUi : popupUi;

                if (screen == null)
                {
                    continue;
                }

                screenCount++;
                Assert.That(
                    root.GetComponentsInChildren<Canvas>(true),
                    Is.Empty,
                    $"{prefabPath} must inherit its Canvas from UiService.");

                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                Assert.That(
                    canvasGroup,
                    Is.Not.Null,
                    $"{prefabPath} requires a root CanvasGroup.");

                SerializedObject serializedScreen = new SerializedObject(screen);
                Assert.That(
                    serializedScreen.FindProperty("canvasGroup").objectReferenceValue,
                    Is.SameAs(canvasGroup),
                    $"{prefabPath} must explicitly bind its root CanvasGroup.");
            }

            Assert.That(screenCount, Is.EqualTo(EXPECTED_SCREEN_PREFAB_COUNT));
        }

        [Test]
        public void BaseUi_ShowAndHide_UpdateCanvasGroupWithoutDeactivation()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ECONOMY_PREFAB_PATH);

            try
            {
                BaseUi ui = root.GetComponent<BaseUi>();
                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

                ui.Hide();

                Assert.That(root.activeSelf, Is.True);
                Assert.That(ui.IsVisible, Is.False);
                Assert.That(canvasGroup.alpha, Is.Zero);
                Assert.That(canvasGroup.interactable, Is.False);
                Assert.That(canvasGroup.blocksRaycasts, Is.False);

                ui.Show();

                Assert.That(root.activeSelf, Is.True);
                Assert.That(ui.IsVisible, Is.True);
                Assert.That(canvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(canvasGroup.interactable, Is.True);
                Assert.That(canvasGroup.blocksRaycasts, Is.True);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void UiServicePrefab_OwnsDefaultDynamicAndPopupCanvases()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UI_SERVICE_PREFAB_PATH);

            try
            {
                UiService uiService = root.GetComponent<UiService>();

                Assert.That(root.GetComponent<Canvas>(), Is.Null);
                AssertCanvas(root.transform, uiService.DefaultCanvasTransform, "DefaultCanvas", 0);
                AssertCanvas(root.transform, uiService.DynamicCanvasTransform, "DynamicCanvas", 10);
                AssertCanvas(root.transform, uiService.PopupCanvasTransform, "PopupCanvas", 20);
                Assert.That(root.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(3));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ScreenSpecificCanvasGroups_AreExplicitlyBound()
        {
            GameObject reinforcementRoot =
                PrefabUtility.LoadPrefabContents(REINFORCEMENT_PREFAB_PATH);
            GameObject shipBuildRoot = PrefabUtility.LoadPrefabContents(SHIP_BUILD_PREFAB_PATH);

            try
            {
                ReinforcementUi reinforcementUi =
                    reinforcementRoot.GetComponent<ReinforcementUi>();
                SerializedProperty panelCanvasGroup = new SerializedObject(reinforcementUi)
                    .FindProperty("panelCanvasGroup");
                Assert.That(panelCanvasGroup.objectReferenceValue, Is.Not.Null);

                ShipBuildUi shipBuildUi = shipBuildRoot.GetComponent<ShipBuildUi>();
                SerializedProperty pipelineCanvasGroup = new SerializedObject(shipBuildUi)
                    .FindProperty("pipelineView")
                    .FindPropertyRelative("canvasGroup");
                Assert.That(
                    pipelineCanvasGroup.objectReferenceValue,
                    Is.SameAs(shipBuildRoot.GetComponent<CanvasGroup>()));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(reinforcementRoot);
                PrefabUtility.UnloadPrefabContents(shipBuildRoot);
            }
        }

        private static void AssertCanvas(
            Transform serviceRoot,
            Transform actualTransform,
            string expectedName,
            int expectedSortingOrder)
        {
            Assert.That(actualTransform, Is.Not.Null);
            Assert.That(actualTransform.parent, Is.SameAs(serviceRoot));
            Assert.That(actualTransform.name, Is.EqualTo(expectedName));

            Canvas canvas = actualTransform.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(expectedSortingOrder));
            Assert.That(actualTransform.GetComponent<CanvasScaler>(), Is.Not.Null);
            Assert.That(actualTransform.GetComponent<GraphicRaycaster>(), Is.Not.Null);
        }
    }
}
