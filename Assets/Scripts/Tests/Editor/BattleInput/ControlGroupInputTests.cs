using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class ControlGroupInputTests
    {
        private InputActionsProvider _provider;
        private ControlGroupInput _input;
        private SettingsService _settings;
        private Keyboard _keyboard;
        private readonly List<(int Digit, bool Assign)> _requests = new List<(int, bool)>();

        [SetUp]
        public void SetUp()
        {
            _provider = new InputActionsProvider();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _settings = new SettingsService(new MemoryRepository(), new List<ISettingsApplier>());
            _settings.Initialize();
            _input = new ControlGroupInput(_provider, _settings);
            _input.Initialize();
            SetFocus(true);
            _provider.Actions.Battle.Enable();
            State();
            _input.Tick();
            _input.SlotRequested += (digit, assign) => _requests.Add((digit, assign));
            _requests.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _input.LateDispose();
            _provider.Dispose();
            InputSystem.RemoveDevice(_keyboard);
        }

        [TestCase(Key.Digit1, 1)]
        [TestCase(Key.Digit9, 9)]
        [TestCase(Key.Digit0, 0)]
        public void TopRowRecallsOnceAndControlAssigns(Key key, int digit)
        {
            State(key);
            _input.Tick();
            _input.Tick();
            Assert.That(_requests, Is.EqualTo(new[] { (digit, false) }));
            State();
            State(Key.LeftCtrl, key);
            _input.Tick();
            Assert.That(_requests[1], Is.EqualTo((digit, true)));
        }

        [Test]
        public void ModifierReleaseBeforeTickDoesNotTurnAssignmentIntoRecall()
        {
            State(Key.LeftCtrl, Key.Digit3);
            State(Key.Digit3);
            _input.Tick();
            Assert.That(_requests, Is.EqualTo(new[] { (3, true) }));
        }

        [Test]
        public void NumpadExtraModifiersAndLockedMapDoNotRequestSlots()
        {
            State(Key.Numpad1);
            _input.Tick();
            State(Key.LeftShift, Key.Digit1);
            _input.Tick();
            State();
            _provider.Actions.Battle.Disable();
            State(Key.LeftCtrl, Key.Digit2);
            _input.Tick();
            Assert.That(_requests, Is.Empty);
            Assert.That(_input.IsPreviewHeld, Is.False);
        }

        [Test]
        public void UnlockWhileHeldRequiresReleaseBeforeFreshRequest()
        {
            _provider.Actions.Battle.Disable();
            _input.Tick();
            State(Key.LeftCtrl, Key.Digit2);
            _provider.Actions.Battle.Enable();
            _input.Tick();
            Assert.That(_input.IsPreviewHeld, Is.False);
            Assert.That(_requests, Is.Empty);
            State();
            _input.Tick();
            State(Key.LeftCtrl, Key.Digit2);
            _input.Tick();
            Assert.That(_requests, Is.EqualTo(new[] { (2, true) }));
        }

        [Test]
        public void CommandSettingControlsPreviewAndAssignment()
        {
            _settings.Saved.Input.ControlGroupModifier = ControlGroupModifier.Command;
            State(Key.LeftCtrl, Key.Digit1);
            _input.Tick();
            Assert.That(_requests, Is.Empty);
            State();
            State(Key.RightMeta, Key.Digit0);
            Assert.That(_input.IsPreviewHeld, Is.True);
            _input.Tick();
            Assert.That(_requests, Is.EqualTo(new[] { (0, true) }));
        }

        [Test]
        public void FocusReturnRequiresReleaseAndNewPress()
        {
            SetFocus(false);
            State(Key.LeftCtrl, Key.Digit4);
            SetFocus(true);
            _input.Tick();
            Assert.That(_requests, Is.Empty);
            Assert.That(_input.IsPreviewHeld, Is.False);
            State();
            _input.Tick();
            State(Key.Digit4);
            _input.Tick();
            Assert.That(_requests, Is.EqualTo(new[] { (4, false) }));
        }

        private void State(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private void SetFocus(bool focused)
        {
            typeof(ControlGroupInput).GetMethod("HandleFocusChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_input, new object[] { focused });
        }

        private sealed class MemoryRepository : ISettingsRepository
        {
            public SettingsLoadStatus Load(out SettingsData data)
            {
                data = new SettingsData();
                return SettingsLoadStatus.Loaded;
            }
            public void Save(SettingsData data) { }
        }
    }
}
