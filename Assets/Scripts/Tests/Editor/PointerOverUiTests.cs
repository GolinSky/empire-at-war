using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class PointerOverUiTests
    {
        private const string RUN_PLAYER_UPDATES_IN_EDIT_MODE = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
        private const string REINFORCEMENT_UI_PATH = "Assets/Prefabs/Ui/Reinforcement/ReinforcementUi.prefab";
        private static readonly Vector2 SHOP_POSITION = new Vector2(200f, 150f);

        private InputSettings _originalSettings;
        private InputSettings _testSettings;
        private Mouse _mouse;
        private Keyboard _keyboard;
        private InputActionsProvider _provider;
        private PointerInput _pointer;
        private UiHitTestStub _uiHitTest;
        private CameraInput _cameraInput;
        private int _scrollZooms;
        private int _keyZooms;

        [SetUp]
        public void SetUp()
        {
            _originalSettings = InputSystem.settings;
            _testSettings = Object.Instantiate(_originalSettings);
            _testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            _testSettings.SetInternalFeatureFlag(RUN_PLAYER_UPDATES_IN_EDIT_MODE, true);
            InputSystem.settings = _testSettings;
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _provider = new InputActionsProvider();
            _provider.Actions.devices = new InputDevice[] { _mouse, _keyboard };
            _provider.Actions.Pointer.Enable();
            _provider.Actions.Camera.Enable();
            _provider.Actions.Battle.Enable();
            _pointer = new PointerInput(_provider);
            _pointer.Initialize();
            _uiHitTest = new UiHitTestStub();
            _cameraInput = new CameraInput(_pointer, new PointerOverUiService(_pointer, _uiHitTest),
                new CameraPreferencesStub(), _provider);
            _scrollZooms = _keyZooms = 0;
            _cameraInput.ZoomScrolled += _ => _scrollZooms++;
            _cameraInput.Zoomed += _ => _keyZooms++;
        }

        [TearDown]
        public void TearDown()
        {
            _pointer.Dispose();
            _provider.Dispose();
            InputSystem.RemoveDevice(_keyboard);
            InputSystem.RemoveDevice(_mouse);
            InputSystem.settings = _originalSettings;
            Object.DestroyImmediate(_testSettings);
        }

        [Test]
        public void WheelOverUi_ScrollsUiOnly_CameraDoesNotZoom()
        {
            _uiHitTest.UiArea = SHOP_POSITION;

            Scroll(SHOP_POSITION);

            Assert.That(_scrollZooms, Is.Zero);
            Assert.That(_uiHitTest.LastQuery, Is.EqualTo(SHOP_POSITION), "The live cursor position is tested.");
        }

        [Test]
        public void WheelOverWorld_ZoomsCamera()
        {
            _uiHitTest.UiArea = SHOP_POSITION;

            Scroll(new Vector2(600f, 400f));

            Assert.That(_scrollZooms, Is.EqualTo(1));
        }

        [Test]
        public void LeavingUi_ReleasesTheWheelImmediately()
        {
            _uiHitTest.UiArea = SHOP_POSITION;

            Scroll(SHOP_POSITION);
            Scroll(new Vector2(600f, 400f));
            Scroll(SHOP_POSITION);

            Assert.That(_scrollZooms, Is.EqualTo(1), "Only the wheel notch outside the shop zooms; no state is latched.");
        }

        [Test]
        public void KeyboardZoom_IsNotBlockedByCursorOverUi()
        {
            _uiHitTest.UiArea = SHOP_POSITION;
            MoveMouse(SHOP_POSITION);

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
            InputSystem.Update();
            _cameraInput.Tick();

            Assert.That(_keyZooms, Is.EqualTo(1));
        }

        [Test]
        public void NoWheel_DoesNotRaycastUi()
        {
            MoveMouse(SHOP_POSITION);
            _cameraInput.Tick();

            Assert.That(_uiHitTest.Queries, Is.Zero, "UI raycasts run only on frames that scroll.");
        }

        [Test]
        public void PointerOverUi_IsFalseUntilThePointerReportsAPosition()
        {
            var service = new PointerOverUiService(_pointer, _uiHitTest);
            _uiHitTest.UiArea = Vector2.zero;

            Assert.That(service.IsPointerOverUi, Is.False, "The default zero position is not the cursor.");
            Assert.That(_uiHitTest.Queries, Is.Zero);

            _uiHitTest.UiArea = SHOP_POSITION;
            MoveMouse(SHOP_POSITION);
            Assert.That(service.IsPointerOverUi, Is.True);
        }

        // Shop scroll viewports and modal blockers are alpha-0 images: they still receive the wheel, so they are UI.
        [Test]
        public void TransparentRaycastImage_CountsAsUi()
        {
            var root = new GameObject("TransparentUi", typeof(RectTransform), typeof(Image));
            try
            {
                Image image = root.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);

                Assert.That(image.raycastTarget, Is.True);
                Assert.That(image.Raycast(Vector2.zero, null), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // A hidden panel keeps its graphics but drops blocksRaycasts; it must stop eating the wheel.
        [Test]
        public void TransparentImage_InHiddenCanvasGroup_IsNotUi()
        {
            var panel = new GameObject("HiddenPanel", typeof(RectTransform), typeof(CanvasGroup));
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            try
            {
                viewport.transform.SetParent(panel.transform, false);
                Image image = viewport.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                CanvasGroup group = panel.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;

                Assert.That(image.Raycast(Vector2.zero, null), Is.False);

                group.alpha = 1f;
                group.blocksRaycasts = true;
                Assert.That(image.Raycast(Vector2.zero, null), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(viewport);
                Object.DestroyImmediate(panel);
            }
        }

        // The wheel over gaps between shop cards lands on the viewport, so it must be a raycast target,
        // and it must sit under the panel group so hiding the shop stops it blocking the camera.
        [Test]
        public void ShopScrollViewport_BlocksWheelOnlyWhileShopIsVisible()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(REINFORCEMENT_UI_PATH);
            var view = new SerializedObject(root.GetComponent<ReinforcementUi>());
            var scroll = (ScrollRect)view.FindProperty("unitScroll").objectReferenceValue;
            var panelGroup = (CanvasGroup)view.FindProperty("panelCanvasGroup").objectReferenceValue;

            Graphic viewport = scroll.viewport.GetComponent<Graphic>();
            Assert.That(viewport, Is.Not.Null, "The scroll viewport needs a graphic to receive the wheel between cards.");
            Assert.That(viewport.raycastTarget, Is.True);
            Assert.That(scroll.viewport.IsChildOf(panelGroup.transform), Is.True);
        }

        private void Scroll(Vector2 position)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = position, scroll = new Vector2(0f, 120f) });
            InputSystem.Update();
            _cameraInput.Tick();
        }

        private void MoveMouse(Vector2 position)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = position });
            InputSystem.Update();
        }

        private sealed class UiHitTestStub : IUiHitTest
        {
            public Vector2? UiArea { get; set; }
            public Vector2 LastQuery { get; private set; }
            public int Queries { get; private set; }

            public bool IsOverUi(Vector2 screenPosition)
            {
                Queries++;
                LastQuery = screenPosition;
                return UiArea == screenPosition;
            }
        }

        private sealed class CameraPreferencesStub : ICameraPreferences
        {
            public float PanSpeedMultiplier => 1f;
            public float ZoomSpeedMultiplier => 1f;
            public bool EdgeScrolling => false;
            public bool InvertZoom => false;
        }
    }
}
