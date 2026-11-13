using System;
using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.MainMenu.SinglePlayer;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.MainMenu.Tutorial;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameSettingsService = EmpireAtWar.Services.Settings.SettingsService;
using SurvivalSetupModel = EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class MainMenuUiControllerLifecycleTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/MainMenuUi.prefab";

        [Test]
        public void OpenTwice_ReusesViewAndForwardsOneActionPerClick()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            MainMenuUiController controller = null;
            try
            {
                MainMenuUi view = root.GetComponent<MainMenuUi>();
                UiServiceStub uiService = new UiServiceStub(view);
                RouteStub routes = new RouteStub();
                controller = new MainMenuUiController(uiService, new CancelRouterStub(),
                    routes, routes, CreateSettings(), new PopupServiceStub(), new TooltipServiceStub());
                controller.Open();
                controller.Open();
                Button start = (Button)new SerializedObject(view)
                    .FindProperty("startDemoButton").objectReferenceValue;

                start.onClick.Invoke();

                Assert.That(uiService.CreateCount, Is.EqualTo(1));
                Assert.That(routes.OpenCount, Is.EqualTo(1));
                Assert.That(view.IsVisible, Is.True);

                controller.LateDispose();
                controller.LateDispose();
                start.onClick.Invoke();
                Assert.That(routes.OpenCount, Is.EqualTo(1));
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void SceneDestroysViewBeforeController_DisposalRemainsSafe()
        {
            GameObject root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
            MainMenuUiController controller = new MainMenuUiController(
                new UiServiceStub(root.GetComponent<MainMenuUi>()), new CancelRouterStub(),
                new RouteStub(), new RouteStub(), CreateSettings(), new PopupServiceStub(), new TooltipServiceStub());
            try
            {
                controller.Open();
                UnityEngine.Object.DestroyImmediate(root);
                Assert.DoesNotThrow(controller.LateDispose);
            }
            finally
            {
                controller.LateDispose();
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(ConfirmPopupResult.Dismissed, false, false, 0)]
        [TestCase(ConfirmPopupResult.Declined, true, false, 1)]
        [TestCase(ConfirmPopupResult.Accepted, true, true, 1)]
        public void Consent_OnlyPersistsPlayerAnswers(ConfirmPopupResult result, bool hasAnswered,
            bool isShared, int saveCount)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            MainMenuUiController controller = null;
            try
            {
                var repository = new ConsentRepository(false);
                var settings = new GameSettingsService(repository, Array.Empty<ISettingsApplier>());
                settings.Initialize();
                var popups = new PopupServiceStub();
                controller = new MainMenuUiController(new UiServiceStub(root.GetComponent<MainMenuUi>()),
                    new CancelRouterStub(), new RouteStub(), new RouteStub(), settings, popups,
                    new TooltipServiceStub());

                controller.Open();
                Assert.That(popups.Complete, Is.Not.Null);
                popups.Complete(result);

                Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
                Assert.That(settings.Saved.Privacy.HasAnsweredConsent, Is.EqualTo(hasAnswered));
                Assert.That(settings.Saved.Privacy.ShareUsageData, Is.EqualTo(isShared));
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PlayRoute_ReusesViewForwardsModesAndReturnsOnCancel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab");
            PlayRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<PlayUi>();
                var service = new UiServiceStub(view);
                var singlePlayer = new RouteStub();
                var multiplayer = new RouteStub();
                var simulation = new RouteStub();
                var tutorial = new RouteStub();
                controller = new PlayRouteUiController(
                    service, new CancelRouterStub(), singlePlayer, multiplayer, simulation, tutorial);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var singlePlayerButton = (Button)fields.FindProperty("singlePlayerButton").objectReferenceValue;
                var multiplayerButton = (Button)fields.FindProperty("multiplayerButton").objectReferenceValue;
                var simulationButton = (Button)fields.FindProperty("simulationButton").objectReferenceValue;
                var tutorialButton = (Button)fields.FindProperty("tutorialButton").objectReferenceValue;
                var backButton = (Button)fields.FindProperty("backButton").objectReferenceValue;
                singlePlayerButton.onClick.Invoke();
                multiplayerButton.onClick.Invoke();
                simulationButton.onClick.Invoke();
                tutorialButton.onClick.Invoke();
                Assert.That(singlePlayer.OpenCount, Is.EqualTo(1));
                Assert.That(multiplayer.OpenCount, Is.EqualTo(1));
                Assert.That(simulation.OpenCount, Is.EqualTo(1));
                Assert.That(tutorial.OpenCount, Is.EqualTo(1));
                Assert.That(controller.TryCancel(), Is.True);
                Assert.That(view.IsVisible, Is.False);
                Assert.That(controller.IsFocused, Is.False);
                controller.Open();
                Assert.That(service.CreateCount, Is.EqualTo(1));
                backButton.onClick.Invoke();
                Assert.That(view.IsVisible, Is.False);
                controller.LateDispose();
                singlePlayerButton.onClick.Invoke();
                multiplayerButton.onClick.Invoke();
                Assert.That(singlePlayer.OpenCount, Is.EqualTo(1));
                Assert.That(multiplayer.OpenCount, Is.EqualTo(1));
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void SinglePlayerRoute_ForwardsOfflineModesAndReturnsOnCancel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SinglePlayerUi.prefab");
            SinglePlayerRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<SinglePlayerUi>();
                var service = new UiServiceStub(view);
                var skirmish = new RouteStub();
                var survival = new RouteStub();
                var loadGame = new RouteStub();
                controller = new SinglePlayerRouteUiController(
                    service, new CancelRouterStub(), skirmish, survival, loadGame);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var skirmishButton = (Button)fields.FindProperty("skirmishButton").objectReferenceValue;
                var survivalButton = (Button)fields.FindProperty("survivalButton").objectReferenceValue;
                var loadButton = (Button)fields.FindProperty("loadButton").objectReferenceValue;
                var backButton = (Button)fields.FindProperty("backButton").objectReferenceValue;
                skirmishButton.onClick.Invoke();
                survivalButton.onClick.Invoke();
                loadButton.onClick.Invoke();
                Assert.That(skirmish.OpenCount, Is.EqualTo(1));
                Assert.That(survival.OpenCount, Is.EqualTo(1));
                Assert.That(loadGame.OpenCount, Is.EqualTo(1));
                Assert.That(service.CreateCount, Is.EqualTo(1));
                Assert.That(controller.TryCancel(), Is.True);
                Assert.That(view.IsVisible, Is.False);
                controller.Open();
                backButton.onClick.Invoke();
                Assert.That(view.IsVisible, Is.False);
                Assert.That(controller.IsFocused, Is.False);
                controller.LateDispose();
                skirmishButton.onClick.Invoke();
                Assert.That(skirmish.OpenCount, Is.EqualTo(1));
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void LoadRoute_SeparatesModesRejectsOldVersionsAndShowsLoadErrors()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab");
            LoadGameRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<SaveBrowserUi>();
                var saves = new EmptySaves();
                var header = BattleSaveHeader.Create(
                    GameMode.Skirmish, BattleSaveSlot.Quicksave,
                    DateTime.UtcNow, 400f, GameSpeed.Normal);
                var old = JsonUtility.FromJson<BattleSaveHeader>(
                    JsonUtility.ToJson(header).Replace(
                        "\"version\":" + BattleSaveHeader.CURRENT_VERSION, "\"version\":0"));
                saves.Headers = new[] { header, old };
                controller = new LoadGameRouteUiController(
                    new UiServiceStub(view), new CancelRouterStub(), saves, null);
                controller.Open();
                var fields = new SerializedObject(view);
                var status = (TMPro.TMP_Text)fields.FindProperty("status").objectReferenceValue;
                var survival = (Toggle)fields.FindProperty("survivalToggle").objectReferenceValue;
                var skirmish = (Toggle)fields.FindProperty("skirmishToggle").objectReferenceValue;
                var load = (Button)fields.FindProperty("loadButton").objectReferenceValue;
                controller.SelectSave(1);
                Assert.That(status.text, Does.Contain("not supported"));
                Assert.That(load.interactable, Is.False);
                load.onClick.Invoke();
                Assert.That(saves.LoadCount, Is.Zero);
                survival.isOn = true;
                Assert.That(saves.LastMode, Is.EqualTo(GameMode.Survival));
                Assert.That(status.text, Does.Contain("No saves"));
                Assert.That(load.interactable, Is.False);
                skirmish.isOn = true;
                controller.SelectSave(0);
                Assert.That(saves.LoadCount, Is.Zero, "Selecting a row must not load the battle.");
                Assert.That(load.interactable, Is.True);
                load.onClick.Invoke();
                Assert.That(saves.LoadCount, Is.EqualTo(1));
                Assert.That(status.text, Does.Contain("Load failed").And.Contain("Unreadable save"));
                Assert.That(controller.TryCancel(), Is.True);
                Assert.That(view.IsVisible, Is.False);
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void LoadRoute_WhenReturningFromFailedRestore_ShowsPendingLoadErrorAndClearsIt()
        {
            var mainRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            var saveRoot = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab");
            var gameData = ScriptableObject.CreateInstance<GameData>();
            MainMenuUiController mainMenu = null;
            LoadGameRouteUiController loadRoute = null;
            try
            {
                gameData.Battle = new SurvivalSetupModel().CreateBattle();
                gameData.PendingLoadError = "Load failed: Save has 1 capture sites, expected 4.";
                var mainView = mainRoot.GetComponent<MainMenuUi>();
                var saveView = saveRoot.GetComponent<SaveBrowserUi>();
                var playRoute = new RouteStub();
                var singlePlayerRoute = new RouteStub();
                mainMenu = new MainMenuUiController(new UiServiceStub(mainView), new CancelRouterStub(),
                    playRoute, new RouteStub(), CreateSettings(), new PopupServiceStub(), new TooltipServiceStub());
                var saves = new EmptySaves();
                loadRoute = new LoadGameRouteUiController(new UiServiceStub(saveView), new CancelRouterStub(),
                    saves, null, gameData);
                var orchestrator = new MainMenuOrchestrator(mainMenu, playRoute, singlePlayerRoute, loadRoute, gameData);

                orchestrator.Initialize();

                var status = (TMPro.TMP_Text)new SerializedObject(saveView).FindProperty("status").objectReferenceValue;
                Assert.That(playRoute.OpenCount, Is.EqualTo(1));
                Assert.That(singlePlayerRoute.OpenCount, Is.EqualTo(1));
                Assert.That(saveView.IsVisible, Is.True);
                Assert.That(saves.LastMode, Is.EqualTo(GameMode.Survival));
                Assert.That(status.text, Is.EqualTo("Load failed: Save has 1 capture sites, expected 4."));
                Assert.That(gameData.PendingLoadError, Is.Null);
            }
            finally
            {
                if (loadRoute != null) loadRoute.LateDispose();
                if (mainMenu != null) mainMenu.LateDispose();
                UnityEngine.Object.DestroyImmediate(gameData);
                PrefabUtility.UnloadPrefabContents(saveRoot);
                PrefabUtility.UnloadPrefabContents(mainRoot);
            }
        }

        [Test]
        public void LoadRoute_DeleteSelectedUnsupportedSaveAndReuseRowWithoutLoading()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab");
            LoadGameRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<SaveBrowserUi>();
                var header = BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Quicksave,
                    DateTime.UtcNow, 400f, GameSpeed.Normal);
                var old = JsonUtility.FromJson<BattleSaveHeader>(JsonUtility.ToJson(header).Replace(
                    "\"version\":" + BattleSaveHeader.CURRENT_VERSION, "\"version\":0"));
                var remaining = BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Autosave,
                    DateTime.UtcNow, 200f, GameSpeed.Normal);
                var saves = new EmptySaves { Headers = new[] { old, remaining } };
                controller = new LoadGameRouteUiController(new UiServiceStub(view), new CancelRouterStub(), saves, null);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var content = (RectTransform)fields.FindProperty("content").objectReferenceValue;
                var row = content.GetComponentsInChildren<SaveBrowserRow>().First();
                var rowFields = new SerializedObject(row);
                var select = (Button)rowFields.FindProperty("button").objectReferenceValue;
                var rowRemove = (Button)rowFields.FindProperty("removeButton").objectReferenceValue;
                var load = (Button)fields.FindProperty("loadButton").objectReferenceValue;
                var remove = (Button)fields.FindProperty("deleteButton").objectReferenceValue;
                var selectedName = (TMPro.TMP_Text)fields.FindProperty("selectedName").objectReferenceValue;

                Assert.That(old.IsCompatible, Is.False);
                Assert.That(select.interactable, Is.True);
                Assert.That(rowRemove.gameObject.activeSelf, Is.False);
                select.onClick.Invoke();
                Assert.That(selectedName.text, Is.EqualTo(old.Slot.Name));
                Assert.That(load.interactable, Is.False);
                Assert.That(remove.interactable, Is.True);
                remove.onClick.Invoke();

                Assert.That(saves.DeleteCount, Is.EqualTo(1));
                Assert.That(saves.DeletedSlot.Name, Is.EqualTo(BattleSaveSlot.Quicksave.Name));
                Assert.That(saves.DeletedMode, Is.EqualTo(GameMode.Skirmish));
                Assert.That(content.GetComponentsInChildren<SaveBrowserRow>(), Is.EqualTo(new[] { row }));
                Assert.That(load.interactable, Is.True);
                Assert.That(selectedName.text, Is.EqualTo(remaining.Slot.Name));
                remove.onClick.Invoke();

                Assert.That(saves.DeleteCount, Is.EqualTo(2));
                Assert.That(saves.DeletedSlot.Name, Is.EqualTo(BattleSaveSlot.Autosave.Name));
                Assert.That(saves.LoadCount, Is.Zero);
                Assert.That(saves.Headers, Is.Empty);
                Assert.That(content.GetComponentsInChildren<SaveBrowserRow>(), Is.Empty);
                var status = (TMPro.TMP_Text)fields.FindProperty("status").objectReferenceValue;
                Assert.That(status.text, Does.Contain("No saves"));
                Assert.That(load.interactable, Is.False);
                Assert.That(remove.interactable, Is.False);
                Assert.That(view.IsVisible, Is.True);
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private sealed class EmptySaves : IBattleSaveRepository
        {
            public BattleSaveHeader[] Headers = Array.Empty<BattleSaveHeader>();
            public GameMode LastMode;
            public int LoadCount;
            public int DeleteCount;
            public GameMode DeletedMode;
            public BattleSaveSlot DeletedSlot;
            public void Save(BattleSave save) => throw new NotSupportedException();
            public BattleSave Load(GameMode mode, BattleSaveSlot slot)
            {
                LoadCount++;
                throw new System.IO.InvalidDataException("Unreadable save");
            }

            public void Delete(GameMode mode, BattleSaveSlot slot)
            {
                DeleteCount++;
                DeletedMode = mode;
                DeletedSlot = slot;
                Headers = Headers.Where(header => header.Mode != mode || header.Slot.Name != slot.Name).ToArray();
            }
            public System.Collections.Generic.IReadOnlyList<BattleSaveHeader> List(GameMode mode)
            {
                LastMode = mode;
                return mode == GameMode.Skirmish ? Headers : Array.Empty<BattleSaveHeader>();
            }
        }

        private sealed class UiServiceStub : IUiService
        {
            private readonly BaseUi _view;
            public int CreateCount { get; private set; }
            public UiServiceStub(BaseUi view) => _view = view;
            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                CreateCount++;
                return (TUi)(BaseUi)_view;
            }
            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }
        }

        private sealed class RouteStub : IPlayRoute, ISettingsRoute, ISkirmishRoute, ISurvivalRoute, ISimulationRoute, ILoadGameRoute, IOnlineRoute, ITutorialRoute,
            ISinglePlayerRoute
        {
            public int OpenCount { get; private set; }
            public void Open() => OpenCount++;
            public void Close() { }
        }

        private static ISettingsService CreateSettings()
        {
            GameSettingsService settings =
                new GameSettingsService(new ConsentRepository(true), new ISettingsApplier[0]);
            settings.Initialize();
            return settings;
        }

        private sealed class ConsentRepository : ISettingsRepository
        {
            private readonly bool _hasAnswered;
            public int SaveCount { get; private set; }

            public ConsentRepository(bool hasAnswered) => _hasAnswered = hasAnswered;

            public SettingsLoadStatus Load(out SettingsData data)
            {
                data = new SettingsData();
                if (_hasAnswered) data.Privacy.Answer(false);
                return SettingsLoadStatus.Loaded;
            }

            public void Save(SettingsData data) => SaveCount++;
        }

        private sealed class CancelRouterStub : IUiCancelRouter
        {
            public event Action CancelUnhandled { add { } remove { } }
            public void Focus(IUiCancelHandler handler) { }
            public void Unfocus(IUiCancelHandler handler) { }
        }

        private sealed class PopupServiceStub : IConfirmPopupService
        {
            public Action<ConfirmPopupResult> Complete { get; private set; }

            public void ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered)
            {
                Assert.That(Complete, Is.Null, "Only one consent prompt should open.");
                Complete = answered;
            }
        }

        private sealed class TooltipServiceStub : ITooltipService
        {
            public TooltipHandle Show(ITooltipContentProvider provider, TooltipAnchor anchor) =>
                throw new InvalidOperationException("No tooltip is expected in this test.");
            public void Hide(TooltipHandle handle) { }
            public void HideAll() { }
        }
    }
}
