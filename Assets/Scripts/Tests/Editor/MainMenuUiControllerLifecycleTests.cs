using System;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
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
                controller = new EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController(
                    service, new CancelRouterStub(), skirmish, survival, new RouteStub(), new EmptySaves(), null);
                controller.Open();
                controller.Open();
                var fields = new SerializedObject(view);
                var skirmishButton = (Button)fields.FindProperty("skirmishButton").objectReferenceValue;
                var survivalButton = (Button)fields.FindProperty("survivalButton").objectReferenceValue;
                var simulationButton = (Button)fields.FindProperty("simulationButton").objectReferenceValue;
                var backButton = (Button)fields.FindProperty("backButton").objectReferenceValue;
                skirmishButton.onClick.Invoke();
                survivalButton.onClick.Invoke();
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
        public void PlayLoadList_SeparatesModesDisablesOldVersionsAndShowsLoadErrors()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab");
            EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController controller = null;
            try
            {
                var view = root.GetComponent<EmpireAtWar.Entities.MainMenu.Play.PlayUi>();
                var saves = new EmptySaves();
                var header = EmpireAtWar.Services.Saves.BattleSaveHeader.Create(
                    EmpireAtWar.Entities.Game.GameMode.Skirmish, EmpireAtWar.Services.Saves.BattleSaveSlot.Quicksave,
                    DateTime.UtcNow, 400f, EmpireAtWar.Controllers.Game.GameSpeed.Normal);
                var old = JsonUtility.FromJson<EmpireAtWar.Services.Saves.BattleSaveHeader>(
                    JsonUtility.ToJson(header).Replace("\"version\":1", "\"version\":0"));
                saves.Headers = new[] { header, old };
                var routes = new RouteStub();
                controller = new EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController(
                    new UiServiceStub(view), new CancelRouterStub(), routes, routes, routes, saves, null);
                controller.Open();
                var fields = new SerializedObject(view);
                var load = (Button)fields.FindProperty("loadButton").objectReferenceValue;
                var status = (TMPro.TMP_Text)fields.FindProperty("loadStatus").objectReferenceValue;
                var slots = (TMPro.TMP_Dropdown)fields.FindProperty("saveSlots").objectReferenceValue;
                Assert.That(load.interactable, Is.True);
                Assert.That(slots.options.Count, Is.EqualTo(2));
                controller.SelectSave(1);
                Assert.That(load.interactable, Is.False);
                Assert.That(status.text, Does.Contain("not supported"));
                controller.SelectSaveMode(1);
                Assert.That(saves.LastMode, Is.EqualTo(EmpireAtWar.Entities.Game.GameMode.Survival));
                Assert.That(load.interactable, Is.False);
                controller.SelectSaveMode(0);
                controller.LoadSave();
                Assert.That(status.text, Does.Contain("Load failed").And.Contain("Unreadable save"));
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
            public void Save(EmpireAtWar.Services.Saves.BattleSave save) => throw new NotSupportedException();
            public EmpireAtWar.Services.Saves.BattleSave Load(EmpireAtWar.Entities.Game.GameMode mode, EmpireAtWar.Services.Saves.BattleSaveSlot slot) => throw new System.IO.InvalidDataException("Unreadable save");
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

        private sealed class RouteStub : EmpireAtWar.Entities.MainMenu.Play.IPlayRoute, ISettingsRoute, ISkirmishRoute, EmpireAtWar.Entities.MainMenu.Survival.ISurvivalRoute, EmpireAtWar.Entities.MainMenu.Simulation.ISimulationRoute
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
