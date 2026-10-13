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

        private sealed class UiServiceStub : IUiService
        {
            private readonly MainMenuUi _view;
            public int CreateCount { get; private set; }
            public UiServiceStub(MainMenuUi view) => _view = view;
            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                CreateCount++;
                return (TUi)(BaseUi)_view;
            }
            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }
        }

        private sealed class RouteStub : ISkirmishRoute, ISettingsRoute
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
