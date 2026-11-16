using System;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace EmpireAtWar.Services.Input
{
    public sealed class ControlGroupInput : IControlGroupInput, IInitializable, ITickable, ILateDisposable
    {
        private readonly InputActionMap _battle;
        private readonly InputAction[] _digits = new InputAction[ControlGroupBindings.SLOT_COUNT];
        private readonly ISettingsService _settings;
        private readonly InputAction _ctrl;
        private readonly InputAction _command;
        private Selectable[] _selectables = new Selectable[32];
        private bool _focused = true;
        private bool _waitForRelease;
        private int _pendingIndex = -1;
        private bool _pendingAssign;

        public event Action<int, bool> SlotRequested;

        public bool IsPreviewHeld => CanRead() && !_waitForRelease && ChosenModifier.IsPressed() && !HasExtraModifiers();

        private bool UsesCommand => _settings.Saved.Input.ControlGroupModifier == ControlGroupModifier.Command;
        private InputAction ChosenModifier => UsesCommand ? _command : _ctrl;

        public ControlGroupInput(InputActionsProvider provider, ISettingsService settings)
        {
            _settings = settings;
            _battle = provider.Actions.Battle.Get();
            _ctrl = _battle.FindAction("ControlGroupModifierCtrl", true);
            _command = _battle.FindAction("ControlGroupModifierCommand", true);
            for (int index = 0; index < _digits.Length; index++)
                _digits[index] = _battle.FindAction($"ControlGroup{(index + 1) % 10}", true);
        }

        public void Initialize()
        {
            _focused = Application.isFocused;
            foreach (InputAction digit in _digits) digit.performed += HandleDigit;
            Application.focusChanged += HandleFocusChanged;
        }

        public void LateDispose()
        {
            foreach (InputAction digit in _digits) digit.performed -= HandleDigit;
            Application.focusChanged -= HandleFocusChanged;
            _pendingIndex = -1;
        }

        public void Tick()
        {
            if (!CanRead())
            {
                _pendingIndex = -1;
                _waitForRelease = true;
                return;
            }

            if (_waitForRelease)
            {
                if (AnyGroupKeyHeld()) return;
                _waitForRelease = false;
            }

            if (_pendingIndex < 0) return;
            int index = _pendingIndex;
            bool assign = _pendingAssign;
            _pendingIndex = -1;
            SlotRequested?.Invoke((index + 1) % 10, assign);
        }

        private void HandleDigit(InputAction.CallbackContext context)
        {
            if (!CanRead())
            {
                _waitForRelease = true;
                return;
            }
            if (_waitForRelease || HasExtraModifiers()) return;
            int index = Array.IndexOf(_digits, context.action);
            if (_pendingIndex >= 0 && index >= _pendingIndex) return;
            _pendingIndex = index;
            // Capture the chord at key-down, before a modifier release later in the frame.
            Keyboard keyboard = (Keyboard)context.control.device;
            _pendingAssign = UsesCommand
                ? keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed
                : keyboard.ctrlKey.isPressed;
        }

        private bool CanRead()
        {
            if (!_focused || !_battle.enabled) return false;
            if (_selectables.Length < Selectable.allSelectableCount)
                Array.Resize(ref _selectables, Selectable.allSelectableCount);
            int count = Selectable.AllSelectablesNoAlloc(_selectables);
            bool typing = false;
            for (int i = 0; i < count; i++)
            {
                Selectable selectable = _selectables[i];
                typing |= selectable is TMP_InputField text && text.isFocused;
                typing |= selectable is InputField legacyText && legacyText.isFocused;
                _selectables[i] = null;
            }
            return !typing;
        }

        private bool HasExtraModifiers()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.shiftKey.isPressed || keyboard.altKey.isPressed ||
                (UsesCommand ? keyboard.ctrlKey.isPressed : keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed));
        }

        private bool AnyGroupKeyHeld()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            if (keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed) return true;
            foreach (InputAction digit in _digits)
                foreach (UnityEngine.InputSystem.Controls.ButtonControl control in digit.controls)
                    if (control.isPressed) return true;
            return false;
        }

        private void HandleFocusChanged(bool focused)
        {
            _focused = focused;
            _pendingIndex = -1;
            _waitForRelease = true;
        }
    }
}
