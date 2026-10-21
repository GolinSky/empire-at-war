using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
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
                    routes, routes, new TooltipServiceStub());
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
                new RouteStub(), new RouteStub(), new TooltipServiceStub());
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

        [Test]
        public void PlayRoute_ReusesViewForwardsModesAndReturnsOnCancel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab");
            EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<EmpireAtWar.Entities.MainMenu.Play.PlayUi>();
                var service = new UiServiceStub(view);
                var skirmish = new RouteStub();
                var survival = new RouteStub();
                var loadGame = new RouteStub();
                controller = new EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController(
                    service, new CancelRouterStub(), skirmish, survival, new RouteStub(), loadGame);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var skirmishButton = (Button)fields.FindProperty("skirmishButton").objectReferenceValue;
                var survivalButton = (Button)fields.FindProperty("survivalButton").objectReferenceValue;
                var simulationButton = (Button)fields.FindProperty("simulationButton").objectReferenceValue;
                var backButton = (Button)fields.FindProperty("backButton").objectReferenceValue;
                var loadButton = (Button)fields.FindProperty("loadButton").objectReferenceValue;
                skirmishButton.onClick.Invoke();
                survivalButton.onClick.Invoke();
                loadButton.onClick.Invoke();
                Assert.That(loadGame.OpenCount, Is.EqualTo(1));
                Assert.That(skirmish.OpenCount, Is.EqualTo(1));
                Assert.That(survival.OpenCount, Is.EqualTo(1));
                Assert.That(simulationButton.interactable, Is.True);
                Assert.That(controller.TryCancel(), Is.True);
                Assert.That(view.IsVisible, Is.False);
                Assert.That(controller.IsFocused, Is.False);
                controller.Open();
                Assert.That(service.CreateCount, Is.EqualTo(1));
                backButton.onClick.Invoke();
                Assert.That(view.IsVisible, Is.False);
                controller.LateDispose();
                skirmishButton.onClick.Invoke();
                survivalButton.onClick.Invoke();
                loadButton.onClick.Invoke();
                Assert.That(loadGame.OpenCount, Is.EqualTo(1));
                Assert.That(skirmish.OpenCount, Is.EqualTo(1));
                Assert.That(survival.OpenCount, Is.EqualTo(1));
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
            EmpireAtWar.Entities.Saves.LoadGameRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<EmpireAtWar.Entities.Saves.SaveBrowserUi>();
                var saves = new EmptySaves();
                var header = EmpireAtWar.Services.Saves.BattleSaveHeader.Create(
                    EmpireAtWar.Entities.Game.GameMode.Skirmish, EmpireAtWar.Services.Saves.BattleSaveSlot.Quicksave,
                    DateTime.UtcNow, 400f, EmpireAtWar.Controllers.Game.GameSpeed.Normal);
                var old = JsonUtility.FromJson<EmpireAtWar.Services.Saves.BattleSaveHeader>(
                    JsonUtility.ToJson(header).Replace(
                        "\"version\":" + EmpireAtWar.Services.Saves.BattleSaveHeader.CURRENT_VERSION, "\"version\":0"));
                saves.Headers = new[] { header, old };
                controller = new EmpireAtWar.Entities.Saves.LoadGameRouteUiController(
                    new UiServiceStub(view), new CancelRouterStub(), saves, null);
                controller.Open();
                var status = (TMPro.TMP_Text)new SerializedObject(view).FindProperty("status").objectReferenceValue;
                controller.SelectSave(1);
                Assert.That(status.text, Does.Contain("not supported"));
                controller.SelectMode(1);
                Assert.That(saves.LastMode, Is.EqualTo(EmpireAtWar.Entities.Game.GameMode.Survival));
                Assert.That(status.text, Does.Contain("No saves"));
                controller.SelectMode(0);
                controller.SelectSave(0);
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
                gameData.Battle = new EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel().CreateBattle();
                gameData.PendingLoadError = "Load failed: Save has 1 capture sites, expected 4.";
                var mainView = mainRoot.GetComponent<MainMenuUi>();
                var saveView = saveRoot.GetComponent<SaveBrowserUi>();
                var playRoute = new RouteStub();
                mainMenu = new MainMenuUiController(new UiServiceStub(mainView), new CancelRouterStub(),
                    playRoute, new RouteStub(), new TooltipServiceStub());
                var saves = new EmptySaves();
                loadRoute = new LoadGameRouteUiController(new UiServiceStub(saveView), new CancelRouterStub(),
                    saves, null, gameData);
                var orchestrator = new MainMenuOrchestrator(mainMenu, playRoute, loadRoute, gameData);

                orchestrator.Initialize();

                var status = (TMPro.TMP_Text)new SerializedObject(saveView).FindProperty("status").objectReferenceValue;
                Assert.That(playRoute.OpenCount, Is.EqualTo(1));
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
        public void LoadRoute_RemoveButtonsDeleteUnsupportedSaveAndReuseRowWithoutLoading()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab");
            LoadGameRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<SaveBrowserUi>();
                var header = BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Quicksave,
                    DateTime.UtcNow, 400f, EmpireAtWar.Controllers.Game.GameSpeed.Normal);
                var old = JsonUtility.FromJson<BattleSaveHeader>(JsonUtility.ToJson(header).Replace(
                    "\"version\":" + BattleSaveHeader.CURRENT_VERSION, "\"version\":0"));
                var remaining = BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Autosave,
                    DateTime.UtcNow, 200f, EmpireAtWar.Controllers.Game.GameSpeed.Normal);
                var saves = new EmptySaves { Headers = new[] { old, remaining } };
                controller = new LoadGameRouteUiController(new UiServiceStub(view), new CancelRouterStub(), saves, null);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var content = (RectTransform)fields.FindProperty("content").objectReferenceValue;
                var row = content.GetComponentsInChildren<SaveBrowserRow>().First();
                var rowFields = new SerializedObject(row);
                var load = (Button)rowFields.FindProperty("button").objectReferenceValue;
                var remove = (Button)rowFields.FindProperty("removeButton").objectReferenceValue;

                Assert.That(old.IsCompatible, Is.False);
                Assert.That(load.interactable, Is.False);
                Assert.That(remove.interactable, Is.True);
                remove.onClick.Invoke();

                Assert.That(saves.DeleteCount, Is.EqualTo(1));
                Assert.That(saves.DeletedSlot.Name, Is.EqualTo(BattleSaveSlot.Quicksave.Name));
                Assert.That(saves.DeletedMode, Is.EqualTo(GameMode.Skirmish));
                Assert.That(content.GetComponentsInChildren<SaveBrowserRow>(), Is.EqualTo(new[] { row }));
                Assert.That(load.interactable, Is.True);
                remove.onClick.Invoke();

                Assert.That(saves.DeleteCount, Is.EqualTo(2));
                Assert.That(saves.DeletedSlot.Name, Is.EqualTo(BattleSaveSlot.Autosave.Name));
                Assert.That(saves.LoadCount, Is.Zero);
                Assert.That(saves.Headers, Is.Empty);
                Assert.That(content.GetComponentsInChildren<SaveBrowserRow>(), Is.Empty);
                var status = (TMPro.TMP_Text)fields.FindProperty("status").objectReferenceValue;
                Assert.That(status.text, Does.Contain("No saves"));
                Assert.That(view.IsVisible, Is.True);
            }
            finally
            {
                if (controller != null) controller.LateDispose();
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private sealed class EmptySaves : EmpireAtWar.Services.Saves.IBattleSaveRepository
        {
            public EmpireAtWar.Services.Saves.BattleSaveHeader[] Headers = Array.Empty<EmpireAtWar.Services.Saves.BattleSaveHeader>();
            public EmpireAtWar.Entities.Game.GameMode LastMode;
            public int LoadCount;
            public int DeleteCount;
            public GameMode DeletedMode;
            public BattleSaveSlot DeletedSlot;
            public void Save(EmpireAtWar.Services.Saves.BattleSave save) => throw new NotSupportedException();
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
            public System.Collections.Generic.IReadOnlyList<EmpireAtWar.Services.Saves.BattleSaveHeader> List(EmpireAtWar.Entities.Game.GameMode mode)
            {
                LastMode = mode;
                return mode == EmpireAtWar.Entities.Game.GameMode.Skirmish ? Headers : Array.Empty<EmpireAtWar.Services.Saves.BattleSaveHeader>();
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

        private sealed class RouteStub : EmpireAtWar.Entities.MainMenu.Play.IPlayRoute, ISettingsRoute, ISkirmishRoute, EmpireAtWar.Entities.MainMenu.Survival.ISurvivalRoute, EmpireAtWar.Entities.MainMenu.Simulation.ISimulationRoute, EmpireAtWar.Entities.Saves.ILoadGameRoute
        {
            public int OpenCount { get; private set; }
            public void Open() => OpenCount++;
            public void Close() { }
        }

        private sealed class CancelRouterStub : IUiCancelRouter
        {
            public event Action CancelUnhandled { add { } remove { } }
            public void Focus(IUiCancelHandler handler) { }
            public void Unfocus(IUiCancelHandler handler) { }
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
