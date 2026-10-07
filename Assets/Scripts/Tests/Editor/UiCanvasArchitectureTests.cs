using EmpireAtWar.Ui.Base;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Presenters.Game;
using EmpireAtWar.Presenters.Economy;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
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
        private const string CORE_GAME_PREFAB_PATH =
            "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab";
        private const string ECONOMY_PREFAB_PATH =
            "Assets/Prefabs/Ui/Economy/EconomyUi.prefab";
        private const string REINFORCEMENT_PREFAB_PATH =
            "Assets/Prefabs/Ui/Reinforcement/ReinforcementUi.prefab";
        private const string SHIP_BUILD_PREFAB_PATH =
            "Assets/Prefabs/Ui/Factions/ShipBuildUi.prefab";
        private const string FADE_PREFAB_PATH =
            "Assets/Prefabs/Ui/Fade/FadeUi.prefab";

        private const int EXPECTED_SCREEN_PREFAB_COUNT = 19;

        [TestCase("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab")]
        [TestCase("Assets/Prefabs/View/CaptureSites/BattleAsteroidCaptureSite.prefab")]
        public void CaptureSiteBuildOptions_UseScreenCanvasAndIgnoreSiteTransform(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            GameObject uiRoot = PrefabUtility.LoadPrefabContents(UI_SERVICE_PREFAB_PATH);

            try
            {
                CaptureSiteView view = root.GetComponent<CaptureSiteView>();
                UiService uiService = uiRoot.GetComponent<UiService>();
                SerializedObject serializedView = new SerializedObject(view);
                GameObject buildOptions = (GameObject)serializedView.FindProperty("buildOptions").objectReferenceValue;
                Canvas statusCanvas = (Canvas)serializedView.FindProperty("statusCanvas").objectReferenceValue;
                RectTransform canvasTransform = (RectTransform)uiService.DynamicCanvasTransform;
                // Prefab preview scenes do not size overlay canvases to the Game view.
                canvasTransform.sizeDelta = new Vector2(1920f, 1080f);
                canvasTransform.localScale = Vector3.one * 0.75f;

                view.InitializeBuildUi(uiService.DynamicCanvasTransform);
                view.SetVisibility(true, false);
                view.SetBuildOptionsVisible(true);
                Canvas.ForceUpdateCanvases();

                RectTransform optionsTransform = (RectTransform)buildOptions.transform;
                Vector2 tapPosition = RectTransformUtility.WorldToScreenPoint(null,
                    canvasTransform.TransformPoint(canvasTransform.rect.center));
                view.SetBuildOptionsPosition(tapPosition);
                Vector2 panelPosition = RectTransformUtility.WorldToScreenPoint(null, optionsTransform.position);
                Assert.That(panelPosition.x, Is.EqualTo(tapPosition.x).Within(0.01f));
                Assert.That(panelPosition.y,
                    Is.EqualTo(tapPosition.y + 24f * canvasTransform.lossyScale.y).Within(0.01f));

                Vector3 screenPosition = optionsTransform.position;
                Vector3 screenScale = optionsTransform.lossyScale;
                root.transform.SetPositionAndRotation(new Vector3(1000f, 200f, -400f), Quaternion.Euler(45f, 90f, 0f));
                root.transform.localScale = Vector3.one * 20f;
                Canvas.ForceUpdateCanvases();

                Assert.That(optionsTransform.parent, Is.SameAs(uiService.DynamicCanvasTransform));
                Assert.That(uiService.DynamicCanvasTransform.GetComponent<Canvas>().renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(buildOptions.activeInHierarchy, Is.True);
                Assert.That(statusCanvas.gameObject.activeSelf, Is.False);
                Assert.That(optionsTransform.anchorMin, Is.EqualTo(canvasTransform.pivot));
                Assert.That(optionsTransform.anchorMax, Is.EqualTo(canvasTransform.pivot));
                Assert.That(optionsTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(optionsTransform.rect.size, Is.EqualTo(new Vector2(560f, 228f)));
                Assert.That(optionsTransform.position, Is.EqualTo(screenPosition));
                Assert.That(optionsTransform.lossyScale, Is.EqualTo(screenScale));
                Assert.That(buildOptions.GetComponentsInChildren<SiteFacilityOptionView>(true), Has.Length.EqualTo(2));

                foreach (Vector2 canvasPoint in new[] { canvasTransform.rect.min, canvasTransform.rect.max })
                {
                    view.SetBuildOptionsPosition(RectTransformUtility.WorldToScreenPoint(null,
                        canvasTransform.TransformPoint(canvasPoint)));
                    Vector2 position = optionsTransform.anchoredPosition;
                    Assert.That(position.x - optionsTransform.rect.width * 0.5f,
                        Is.GreaterThanOrEqualTo(canvasTransform.rect.xMin));
                    Assert.That(position.x + optionsTransform.rect.width * 0.5f,
                        Is.LessThanOrEqualTo(canvasTransform.rect.xMax));
                    Assert.That(position.y, Is.GreaterThanOrEqualTo(canvasTransform.rect.yMin));
                    Assert.That(position.y + optionsTransform.rect.height,
                        Is.LessThanOrEqualTo(canvasTransform.rect.yMax));
                }

                view.SetBuildOptionsVisible(false);
                Assert.That(buildOptions.activeSelf, Is.False);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                PrefabUtility.UnloadPrefabContents(uiRoot);
            }
        }

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

                if (baseUi == null)
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

                SerializedObject serializedScreen = new SerializedObject(baseUi);
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

        [Test]
        public void EconomyAndReinforcementControllers_AreSkirmishUiRoutes()
        {
            Assert.That(
                typeof(ISkirmishUiRoute).IsAssignableFrom(
                    typeof(EconomyUiController)),
                Is.True);
            Assert.That(
                typeof(ISkirmishUiRoute).IsAssignableFrom(
                    typeof(ReinforcementUiController)),
                Is.True);
        }

        [Test]
        public void CoreGameUiController_OwnsSkirmishRoutesAndPassiveView()
        {
            Assert.That(
                typeof(ISkirmishRouteNavigation).IsAssignableFrom(typeof(CoreGameUiController)),
                Is.True);
            Assert.That(
                typeof(ISkirmishRouteNavigation).IsAssignableFrom(typeof(SkirmishOrchestrator)),
                Is.False);
            Assert.That(typeof(CoreGameUi).BaseType, Is.EqualTo(typeof(BaseUi)));
            Assert.That(typeof(ICoreGameUi).IsAssignableFrom(typeof(CoreGameUi)), Is.True);
        }

        [Test]
        public void CoreGameUiPrefab_BindsReinforcementRouteButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                CORE_GAME_PREFAB_PATH);

            try
            {
                CoreGameUi coreGameUi = root.GetComponent<CoreGameUi>();
                Button expectedButton = root.transform
                    .Find("MiddlePanel/ReinforcementButton")
                    .GetComponent<Button>();
                SerializedProperty reinforcementButton =
                    new SerializedObject(coreGameUi)
                        .FindProperty("reinforcementButton");

                Assert.That(reinforcementButton, Is.Not.Null);
                Assert.That(
                    reinforcementButton.objectReferenceValue,
                    Is.SameAs(expectedButton));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void FadeUiPrefab_HasAssignedReferencesAndCoverBlocksInput()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FADE_PREFAB_PATH);

            try
            {
                FadeUi ui = root.GetComponent<FadeUi>();
                Assert.That(ui, Is.InstanceOf<IFadeUi>());
                SerializedObject serializedUi = new SerializedObject(ui);
                Image fadeImage = (Image)serializedUi.FindProperty("fadeImage").objectReferenceValue;
                Assert.That(fadeImage, Is.Not.Null, "FadeUi.fadeImage must be assigned in the prefab.");
                Assert.That(fadeImage.raycastTarget, Is.True);

                ui.Cover();

                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                Assert.That(fadeImage.color.a, Is.EqualTo(1f));
                Assert.That(canvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(canvasGroup.blocksRaycasts, Is.True);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void CoreGameUiPrefab_InitializesWithAssignedReferencesAndForwardsButtons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CORE_GAME_PREFAB_PATH);

            try
            {
                CoreGameUi ui = root.GetComponent<CoreGameUi>();
                Assert.That(ui, Is.InstanceOf<ICoreGameUi>());
                SerializedObject serializedUi = new SerializedObject(ui);
                string[] requiredReferences =
                {
                    "canvasGroup", "timeButton", "speedUpButton", "reinforcementButton", "videoModeButton",
                    "timeImage", "speedUpImage", "panelImage", "miniMapRouteParent",
                    "contentRouteParent", "buildPipelineRouteParent", "contentGrid",
                    "contentSizeFitter", "contentScroll", "endGameUi"
                };
                foreach (string field in requiredReferences)
                {
                    Assert.That(serializedUi.FindProperty(field).objectReferenceValue,
                        Is.Not.Null, $"CoreGameUi.{field} must be assigned in the prefab.");
                }

                CoreGamePresenterStub presenter = new CoreGamePresenterStub();
                ui.SetPresenter(presenter);
                ui.Initialize();
                ui.SetTimeControls(true, GameSpeed.Fast);
                ui.SetTimeControls(false, GameSpeed.Normal);

                Assert.That(((Image)serializedUi.FindProperty("timeImage").objectReferenceValue).sprite,
                    Is.Not.Null);
                Assert.That(((Image)serializedUi.FindProperty("speedUpImage").objectReferenceValue).sprite,
                    Is.Not.Null);

                ((Button)serializedUi.FindProperty("timeButton").objectReferenceValue).onClick.Invoke();
                ((Button)serializedUi.FindProperty("speedUpButton").objectReferenceValue).onClick.Invoke();
                ((Button)serializedUi.FindProperty("reinforcementButton").objectReferenceValue).onClick.Invoke();
                ((Button)serializedUi.FindProperty("videoModeButton").objectReferenceValue).onClick.Invoke();
                Assert.That(presenter.PlayCount, Is.EqualTo(1));
                Assert.That(presenter.SpeedUpCount, Is.EqualTo(1));
                Assert.That(presenter.ReinforcementCount, Is.EqualTo(1));
                Assert.That(presenter.CinematicCount, Is.EqualTo(1));

                ui.Dispose();
                ((Button)serializedUi.FindProperty("timeButton").objectReferenceValue).onClick.Invoke();
                Assert.That(presenter.PlayCount, Is.EqualTo(1));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ReinforcementUiPrefab_RemovesLegacySwitchButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                REINFORCEMENT_PREFAB_PATH);

            try
            {
                ReinforcementUi reinforcementUi =
                    root.GetComponent<ReinforcementUi>();
                SerializedObject serializedUi =
                    new SerializedObject(reinforcementUi);

                Assert.That(root.transform.Find("RoundButtonCyan"), Is.Null);
                Assert.That(
                    serializedUi.FindProperty("switchButton"),
                    Is.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
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

        private sealed class CoreGamePresenterStub : ICoreGamePresenter
        {
            public int PlayCount { get; private set; }
            public int SpeedUpCount { get; private set; }
            public int ReinforcementCount { get; private set; }
            public int CinematicCount { get; private set; }

            public void Play() => PlayCount++;

            public void SpeedUp() => SpeedUpCount++;

            public void ToggleReinforcement() => ReinforcementCount++;

            public void StartCinematic() => CinematicCount++;

            public void ClearFleetSelection() { }
        }
    }
}
