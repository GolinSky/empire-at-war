using EmpireAtWar.Ui.Base;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.TutorialUi;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
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

        [TestCase("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab")]
        public void CaptureSites_KeepOnlyWorldStatusUi(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Assert.That(root.GetComponentsInChildren<SiteFacilityOptionView>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(1));
                Assert.That(root.GetComponent<CaptureSiteView>(), Is.Not.Null);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        [Test]
        public void UiScreenPrefabs_UseBoundCanvasGroupsAndInheritTheirScreenCanvas()
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
                var serializedScreen = new SerializedObject(baseUi);
                Canvas[] localCanvases = root.GetComponentsInChildren<Canvas>(true);
                if (baseUi is HintUi)
                {
                    var marker = (HintWorldMarkerView)serializedScreen.FindProperty("worldMarker").objectReferenceValue;
                    Assert.That(marker, Is.Not.Null);
                    Assert.That(marker.transform.parent, Is.SameAs(root.transform));
                    Assert.That(localCanvases, Has.Length.EqualTo(1));
                    Canvas markerCanvas = localCanvases[0];
                    Assert.That(markerCanvas.gameObject, Is.SameAs(marker.gameObject));
                    Assert.That(markerCanvas.overrideSorting, Is.True);
                    Assert.That(markerCanvas.sortingOrder, Is.EqualTo(-5));
                }
                else
                {
                    Assert.That(localCanvases, Is.Empty, $"{prefabPath} must inherit its Canvas from UiService.");
                }

                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                Assert.That(
                    canvasGroup,
                    Is.Not.Null,
                    $"{prefabPath} requires a root CanvasGroup.");

                Assert.That(
                    serializedScreen.FindProperty("canvasGroup").objectReferenceValue,
                    Is.SameAs(canvasGroup),
                    $"{prefabPath} must explicitly bind its root CanvasGroup.");
            }

            Assert.That(screenCount, Is.GreaterThan(0), "The UI prefab audit must inspect at least one screen.");
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
                UiPlacement placement = root.GetComponent<UiPlacement>();
                Assert.That(uiService.Placement, Is.SameAs(placement));
                SerializedObject serializedPlacement = new SerializedObject(placement);
                foreach (string field in new[] { "defaultCanvas", "dynamicCanvas", "popupCanvas", "feedbackCanvas",
                    "miniMapSlot", "buildPipelineSlot", "economySlot", "factionLayout", "shipLayout",
                    "contextualSlot", "endGameSlot", "modalSlot", "tooltipSlot", "fadeSlot" })
                    Assert.That(serializedPlacement.FindProperty(field).objectReferenceValue, Is.Not.Null, field);

                Assert.That(root.GetComponent<Canvas>(), Is.Null);
                AssertCanvas(root.transform, root.transform.Find("DefaultCanvas"), "DefaultCanvas", 0);
                AssertCanvas(root.transform, root.transform.Find("DynamicCanvas"), "DynamicCanvas", 10);
                AssertCanvas(root.transform, root.transform.Find("PopupCanvas"), "PopupCanvas", 20);
                Transform feedback = root.transform.Find("FeedbackCanvas");
                Assert.That(feedback.parent, Is.SameAs(root.transform));
                Assert.That(feedback.GetComponent<Canvas>().sortingOrder, Is.EqualTo(-10));
                Assert.That(feedback.GetComponent<GraphicRaycaster>(), Is.Null);
                Assert.That(root.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(4));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase("CoreGameUi", "defaultCanvas")]
        [TestCase("MainMenuUi", "defaultCanvas")]
        [TestCase("SkirmishUi", "defaultCanvas")]
        [TestCase("ReinforcementUi", "defaultCanvas")]
        [TestCase("HeroUi", "defaultCanvas")]
        [TestCase("EconomyUi", "economySlot")]
        [TestCase("FactionUi", "factionLayout")]
        [TestCase("ShipUi", "shipLayout")]
        [TestCase("ShipGroupUi", "shipLayout")]
        [TestCase("MiniMapUi", "miniMapSlot")]
        [TestCase("ShipBuildUi", "buildPipelineSlot")]
        [TestCase("FpsUi", "dynamicCanvas")]
        [TestCase("ControlGroupUi", "dynamicCanvas")]
        [TestCase("UnitOrderFeedbackUi", "feedbackCanvas")]
        [TestCase("CaptureSiteBuildUi", "contextualSlot")]
        [TestCase("EndGameUi", "endGameSlot")]
        [TestCase("PauseMenuUi", "modalSlot")]
        [TestCase("SettingsUi", "modalSlot")]
        [TestCase("AcceptPopupUi", "modalSlot")]
        [TestCase("PopupBlockerUi", "modalSlot")]
        [TestCase("SteamInvitePopupUi", "modalSlot")]
        [TestCase("TooltipUi", "tooltipSlot")]
        [TestCase("FadeUi", "fadeSlot")]
        public void UiPlacement_PreservesEveryScreenParent(string screenName, string field)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UI_SERVICE_PREFAB_PATH);
            try
            {
                UiPlacement placement = root.GetComponent<UiPlacement>();
                Object reference = new SerializedObject(placement).FindProperty(field).objectReferenceValue;
                Transform expected = reference is UiContentLayout layout
                    ? screenName == "FactionUi" ? layout.Viewport : layout.Content
                    : ((Component)reference).transform;
                System.Type screenType = System.Array.Find(typeof(BaseUi).Assembly.GetTypes(),
                    type => type.Name == screenName && typeof(BaseUi).IsAssignableFrom(type));
                object actual = typeof(UiPlacement).GetMethod("GetParent").MakeGenericMethod(screenType)
                    .Invoke(placement, null);
                Assert.That(actual, Is.SameAs(expected));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
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

        [TestCase(SelectionType.Base, true)]
        [TestCase(SelectionType.MiningFacility, false)]
        [TestCase(SelectionType.DefendPlatform, false)]
        [TestCase(SelectionType.None, false)]
        public void CoreGameUiController_ShowsProductionContentForSelectedStations(
            SelectionType selectionType, bool expectedVisible)
        {
            GameObject serviceRoot = PrefabUtility.LoadPrefabContents(UI_SERVICE_PREFAB_PATH);
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(CORE_GAME_PREFAB_PATH);
            try
            {
                UiService service = serviceRoot.GetComponent<UiService>();
                CoreGameUi hud = hudRoot.GetComponent<CoreGameUi>();
                var controller = new CoreGameUiController(
                    uiService: service, placement: service.Placement, cancelRouter: null,
                    selectionService: null, battleState: null, gameSpeed: null, skirmishFlow: null,
                    battleVictoryNotifier: null, cinematicCamera: null, tooltipService: null,
                    bindings: null, session: null, battle: null, clock: null, localPlayer: null, pointer: null);
                typeof(CoreGameUiController).GetField("_ui", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, hud);
                typeof(CoreGameUiController).GetMethod("UpdateContentVisibility",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, new object[] { new ProductionSelectionContext(selectionType) });

                Assert.That(service.Placement.FactionLayout.gameObject.activeSelf, Is.EqualTo(expectedVisible));
                Assert.That(((UiPlacement)service.Placement).ShipLayout.gameObject.activeSelf, Is.False);
                var panel = (Image)new SerializedObject(hud).FindProperty("panelImage").objectReferenceValue;
                Assert.That(panel.gameObject.activeSelf, Is.EqualTo(expectedVisible));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
                PrefabUtility.UnloadPrefabContents(serviceRoot);
            }
        }

        private sealed class ProductionSelectionContext : ISelectionContext
        {
            public ProductionSelectionContext(SelectionType selectionType) => SelectionType = selectionType;
            public IEntity Entity => null;
            public IReadOnlyList<IEntity> Entities => System.Array.Empty<IEntity>();
            public IEntitySelectionFacade SelectionFacade => null;
            public SelectionType SelectionType { get; }
            public bool HasSelectable => SelectionType != SelectionType.None;
            public int Count => HasSelectable ? 1 : 0;
            public SelectionScope Scope => SelectionScope.Local;
            public bool Contains(IEntity entity) => false;
        }

        [Test]
        public void FadeUiPrefab_HasAssignedReferencesAndCoverBlocksInput()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FADE_PREFAB_PATH);

            try
            {
                FadeUi ui = root.GetComponent<FadeUi>();
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
                SerializedObject serializedUi = new SerializedObject(ui);
                string[] requiredReferences =
                {
                    "canvasGroup", "timeButton", "speedUpButton", "reinforcementButton", "videoModeButton",
                    "timeImage", "speedUpImage", "panelImage", "contextualPopup", "productionMenus"
                };
                foreach (string field in requiredReferences)
                {
                    Assert.That(serializedUi.FindProperty(field).objectReferenceValue,
                        Is.Not.Null, $"CoreGameUi.{field} must be assigned in the prefab.");
                }

                CoreGamePresenterStub presenter = new CoreGamePresenterStub();
                ui.AssignPresenter(presenter);
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

                ui.SetProductionMenusAvailable(true);
                var menus = ui.ProductionMenus;
                var serializedMenus = new SerializedObject(menus);
                Button research = (Button)serializedMenus.FindProperty("researchButton").objectReferenceValue;
                Button structures = (Button)serializedMenus.FindProperty("structuresButton").objectReferenceValue;
                Button close = (Button)serializedMenus.FindProperty("closeButton").objectReferenceValue;
                foreach (Button launcher in new[]
                         {
                             (Button)serializedUi.FindProperty("reinforcementButton").objectReferenceValue,
                             (Button)serializedUi.FindProperty("videoModeButton").objectReferenceValue,
                             (Button)new SerializedObject((MonoBehaviour)ui.SuperWeaponsView)
                                 .FindProperty("orbitalButton").objectReferenceValue,
                             research, structures
                         })
                {
                    Rect rect = ((RectTransform)launcher.transform).rect;
                    Assert.That(rect.width, Is.EqualTo(rect.height));
                    Assert.That(rect.width, Is.LessThan(66f));
                    var icon = (RectTransform)launcher.transform.Find("Icon");
                    Assert.That(icon, Is.Not.Null, launcher.name);
                    bool usesGeneratedIcon = launcher == research || launcher == structures;
                    float iconSize = usesGeneratedIcon ? 23f : 16.1f;
                    Assert.That(icon.rect.width * icon.localScale.x, Is.EqualTo(iconSize).Within(0.01f), launcher.name);
                    Assert.That(icon.rect.height * icon.localScale.y, Is.EqualTo(iconSize).Within(0.01f), launcher.name);
                    if (usesGeneratedIcon)
                        Assert.That(icon.GetComponent<Image>().sprite, Is.Not.Null, launcher.name);
                }

                research.onClick.Invoke();
                Assert.That(menus.IsOpen && menus.ResearchCards.gameObject.activeSelf, Is.True);
                Assert.That(menus.StructureCards.gameObject.activeSelf, Is.False);
                structures.onClick.Invoke();
                Assert.That(menus.IsOpen && menus.StructureCards.gameObject.activeSelf, Is.True);
                Assert.That(menus.ResearchCards.gameObject.activeSelf, Is.False);
                close.onClick.Invoke();
                Assert.That(menus.IsOpen, Is.False);
                structures.onClick.Invoke();
                ui.SetMode("Spectator", true);
                Assert.That(menus.IsOpen || research.gameObject.activeSelf || structures.gameObject.activeSelf, Is.False);

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
