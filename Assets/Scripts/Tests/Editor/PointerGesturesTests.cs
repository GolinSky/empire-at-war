using System;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class PointerGesturesTests
    {
        private const string RUN_PLAYER_UPDATES_IN_EDIT_MODE = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
        private InputActionsProvider _provider;
        private Mouse _mouse;
        private InputSettings _originalSettings;
        private InputSettings _testSettings;
        private PointerStub _pointer;
        private AbilityStub _abilities;
        private UnitActionTargetingModel _targeting;
        private SuperWeaponTargetingModel _superWeapons;
        private PointerGestures _gestures;
        private MarqueeSelectionPresenter _marquee;
        private MarqueeViewStub _view;
        private int _pressed;
        private int _clicked;
        private int _commanded;
        private int _completed;

        [SetUp]
        public void SetUp()
        {
            _originalSettings = InputSystem.settings;
            _testSettings = UnityEngine.Object.Instantiate(_originalSettings);
            _testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            // Editor updates do not drive gameplay action press/release transitions.
            _testSettings.SetInternalFeatureFlag(RUN_PLAYER_UPDATES_IN_EDIT_MODE, true);
            InputSystem.settings = _testSettings;
            _mouse = InputSystem.AddDevice<Mouse>();
            _provider = new InputActionsProvider();
            _provider.Actions.devices = new InputDevice[] { _mouse };
            _provider.Actions.Battle.Enable();
            _pointer = new PointerStub();
            _abilities = new AbilityStub();
            _targeting = new UnitActionTargetingModel();
            _superWeapons = new SuperWeaponTargetingModel();
            _gestures = new PointerGestures(_pointer, new UiHitTestStub(), _provider,
                _abilities, _targeting, _superWeapons);
            _view = new MarqueeViewStub();
            _marquee = new MarqueeSelectionPresenter(_gestures, _view,
                new MarqueeSelectionModel(), new HudStub());
            _marquee.Initialize();
            _pressed = _clicked = _commanded = _completed = 0;
            _gestures.WorldPressed += _ => _pressed++;
            _gestures.WorldClicked += _ => _clicked++;
            _gestures.WorldCommanded += _ => _commanded++;
            _marquee.Completed += _ => _completed++;
        }

        [TearDown]
        public void TearDown()
        {
            _marquee.LateDispose();
            _provider.Dispose();
            InputSystem.RemoveDevice(_mouse);
            InputSystem.settings = _originalSettings;
            UnityEngine.Object.DestroyImmediate(_testSettings);
        }

        [Test]
        public void PendingAbility_LeftPressCancelsAndConsumesSelectionGesture()
        {
            Arm("ability");
            MouseFrame(1, Vector2.zero);
            Assert.That(_abilities.IsWaitingForTarget, Is.False);

            MouseFrame(1, new Vector2(100f, 100f));
            MouseFrame(0, new Vector2(100f, 100f));
            Assert.That(_pressed, Is.Zero);
            Assert.That(_clicked, Is.Zero);
            Assert.That(_commanded, Is.Zero);
            Assert.That(_completed, Is.Zero);
            Assert.That(_view.Visible, Is.False);

            MouseFrame(1, Vector2.zero);
            MouseFrame(0, Vector2.zero);
            Assert.That(_pressed, Is.EqualTo(1));
            Assert.That(_clicked, Is.EqualTo(1));
        }

        [TestCase("ability")]
        [TestCase("superweapon")]
        [TestCase("Attack")]
        [TestCase("Guard")]
        [TestCase("AttackMove")]
        [TestCase("WaypointMove")]
        public void PendingTarget_LeftClickAndDragPreserveSelection_RightClickStillCommands(string mode)
        {
            Arm(mode);
            MouseFrame(1, Vector2.zero);
            MouseFrame(1, new Vector2(100f, 100f));
            MouseFrame(0, new Vector2(100f, 100f));
            Assert.That(_pressed, Is.Zero);
            Assert.That(_clicked, Is.Zero);
            Assert.That(_completed, Is.Zero);
            Assert.That(_view.Visible, Is.False);

            MouseFrame(2, Vector2.zero);
            MouseFrame(0, Vector2.zero);
            Assert.That(_commanded, Is.EqualTo(1));
        }

        [Test]
        public void DisabledBattleMap_BlocksSelectionAndCommands()
        {
            _provider.Actions.Battle.Disable();
            MouseFrame(1, Vector2.zero);
            MouseFrame(1, new Vector2(100f, 100f));
            MouseFrame(0, new Vector2(100f, 100f));
            MouseFrame(2, Vector2.zero);
            MouseFrame(0, Vector2.zero);
            Assert.That(_pressed, Is.Zero);
            Assert.That(_clicked, Is.Zero);
            Assert.That(_commanded, Is.Zero);
            Assert.That(_completed, Is.Zero);
            Assert.That(_view.Visible, Is.False);
        }

        [Test]
        public void DisabledSelection_QuickLoadStillEnabled_BlocksGameplayAndCancelsDrag()
        {
            MouseFrame(1, Vector2.zero);
            MouseFrame(1, new Vector2(100f, 100f));
            Assert.That(_view.Visible, Is.True);
            _provider.Actions.Battle.Disable();
            _provider.Actions.Battle.QuickLoad.Enable();
            Assert.That(_provider.Actions.Battle.enabled, Is.True);
            MouseFrame(0, new Vector2(100f, 100f));
            Assert.That(_view.Visible, Is.False);
            int pressesBefore = _pressed;
            MouseFrame(1, Vector2.zero);
            MouseFrame(0, Vector2.zero);
            MouseFrame(2, Vector2.zero);
            MouseFrame(0, Vector2.zero);
            Assert.That(_pressed, Is.EqualTo(pressesBefore));
            Assert.That(_clicked, Is.Zero);
            Assert.That(_commanded, Is.Zero);
            Assert.That(_completed, Is.Zero);
        }

        [TestCase("disabled")]
        [TestCase("ability")]
        [TestCase("superweapon")]
        [TestCase("Attack")]
        public void InterruptedDrag_HidesMarqueeWithoutCompletingSelection(string interruption)
        {
            MouseFrame(1, Vector2.zero);
            MouseFrame(1, new Vector2(100f, 100f));
            Assert.That(_view.Visible, Is.True);

            if (interruption == "disabled") _provider.Actions.Battle.Disable();
            else Arm(interruption);
            MouseFrame(1, new Vector2(110f, 110f));
            Assert.That(_view.Visible, Is.False);
            MouseFrame(0, new Vector2(110f, 110f));
            Assert.That(_completed, Is.Zero);
            Assert.That(_clicked, Is.Zero);
        }

        [Test]
        public void UninterruptedDrag_CompletesSelectionAndHidesMarquee()
        {
            MouseFrame(1, Vector2.zero);
            MouseFrame(1, new Vector2(100f, 100f));
            Assert.That(_view.Visible, Is.True);
            MouseFrame(0, new Vector2(100f, 100f));
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_view.Visible, Is.False);
        }

        private void Arm(string mode)
        {
            if (mode == "ability") _abilities.IsWaitingForTarget = true;
            else if (mode == "superweapon") _superWeapons.Start(SuperWeaponType.IonCannon);
            else _targeting.Start((UnitActionId)Enum.Parse(typeof(UnitActionId), mode));
        }

        private void MouseFrame(ushort buttons, Vector2 position)
        {
            _pointer.Position = position;
            InputSystem.QueueStateEvent(_mouse, new MouseState { buttons = buttons, position = position });
            InputSystem.Update();
            _gestures.Tick();
        }

        private sealed class PointerStub : IPointerInput
        {
            public event Action<Vector2> PrimaryPressed { add { } remove { } }
            public event Action<Vector2> PrimaryReleased { add { } remove { } }
            public Vector2 Position { get; set; }
            public bool HasPosition => true;
            public int ClickCount => 1;
        }

        private sealed class AbilityStub : IShipAbilityTargeting
        {
            public event Action TargetingChanged { add { } remove { } }
            public bool IsWaitingForTarget { get; set; }
            public bool IsValidTarget(IEntity target) => true;
            public void SubmitTarget(IEntity target) { }
            public void CancelTargeting() => IsWaitingForTarget = false;
        }

        private sealed class UiHitTestStub : IUiHitTest
        {
            public bool IsOverUi(Vector2 position) => false;
        }

        private sealed class HudStub : IHudVisibilityObserver
        {
            public bool IsHudVisible => true;
        }

        private sealed class MarqueeViewStub : IMarqueeSelectionView
        {
            public bool Visible { get; private set; }
            public void Show(MarqueeRectangle rectangle) => Visible = true;
            public void Hide() => Visible = false;
        }
    }
}
