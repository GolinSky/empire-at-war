using EmpireAtWar.Lifecycle;
using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Rebinds actions of the shared <see cref="GameInputActions"/> and applies saved overrides from settings.
    /// </summary>
    public sealed class InputBindingService : IInputBindings, ISettingsApplier, IOrderedDisposable
    {
        private const string UNBOUND_LABEL = "Unbound";
        private const string CANCEL_REBIND_PATH = "<Keyboard>/escape";
        private const string POINTER_POSITION_PATH = "<Pointer>/position";
        private const string POINTER_DELTA_PATH = "<Pointer>/delta";
        private const string CTRL_PATH = "<Keyboard>/ctrl";
        private const string SHIFT_PATH = "<Keyboard>/shift";
        private const string ALT_PATH = "<Keyboard>/alt";

        // A chord captures its key with these excluded, then reads which modifiers are held.
        // Rebinding swallows every event that yields a candidate, so a modifier press must match nothing
        // (anyKey included) to reach the keyboard state.
        private static readonly string[] CHORD_EXCLUDED_PATHS =
        {
            CTRL_PATH, "<Keyboard>/leftCtrl", "<Keyboard>/rightCtrl",
            SHIFT_PATH, "<Keyboard>/leftShift", "<Keyboard>/rightShift",
            ALT_PATH, "<Keyboard>/leftAlt", "<Keyboard>/rightAlt",
            "<Keyboard>/anyKey",
        };

        private readonly InputActionAsset _asset;
        private readonly List<BindingSlot> _slots;
        private readonly List<InputActionMap> _suspendedMaps = new List<InputActionMap>();
        private List<BindingSlot> _pendingConflicts = new List<BindingSlot>();
        private InputActionRebindingExtensions.RebindingOperation _operation;

        private string[] _previousOverridePaths;
        private string[] _previousEffectivePaths;
        private List<string> _heldModifierPaths;

        private BindingSlot _reboundSlot;

        public event Action BindingsChanged;

        public IReadOnlyList<BindingSlot> RebindableSlots => _slots;
        public IReadOnlyList<BindingSlot> PendingConflicts => _pendingConflicts;

        public InputBindingService(InputActionsProvider provider)
        {
            _asset = provider.Actions.asset;
            _slots = BindingSlotCatalog.Build(provider.Actions.Camera.Get(), provider.Actions.Battle.Get());
        }

        public void Dispose()
        {
            if (_operation != null)
            {
                FinishRebind();
            }
        }

        public void Apply(SettingsData settings)
        {
            string overridesJson = settings.Input.BindingOverridesJson;
            if (ExportOverrides() == overridesJson)
            {
                return;
            }

            _asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(overridesJson))
            {
                _asset.LoadBindingOverridesFromJson(overridesJson);
            }

            BindingsChanged?.Invoke();
        }

        public string GetBindingDisplayString(BindingSlot slot)
        {
            for (int i = slot.FirstPartIndex; i < slot.FirstPartIndex + slot.PartCount; i++)
            {
                if (string.IsNullOrEmpty(slot.Action.bindings[i].effectivePath))
                {
                    return UNBOUND_LABEL;
                }
            }

            // A chord's composite binding formats every part, e.g. "Ctrl+Shift+A".
            return slot.Action.GetBindingDisplayString(slot.BindingIndex);
        }

        public void StartRebind(BindingSlot slot, Action<RebindResult> completed)
        {
            if (_operation != null || _pendingConflicts.Count > 0)
            {
                throw new InvalidOperationException("A rebind is already in progress.");
            }

            _reboundSlot = slot;
            _previousOverridePaths = new string[slot.PartCount];
            _previousEffectivePaths = new string[slot.PartCount];
            for (int part = 0; part < slot.PartCount; part++)
            {
                InputBinding binding = slot.Action.bindings[slot.FirstPartIndex + part];
                _previousOverridePaths[part] = binding.overridePath;
                _previousEffectivePaths[part] = binding.effectivePath;
            }

            // Capture owns the keyboard: no gameplay or UI action (Escape included) may react meanwhile.
            SuspendEnabledMaps();
            _operation = slot.Action.PerformInteractiveRebinding(slot.KeyPartIndex)
                .WithControlsExcluding(POINTER_POSITION_PATH)
                .WithControlsExcluding(POINTER_DELTA_PATH)
                .WithCancelingThrough(CANCEL_REBIND_PATH);
            if (slot.IsChord)
            {
                foreach (string excludedPath in CHORD_EXCLUDED_PATHS)
                {
                    _operation.WithControlsExcluding(excludedPath);
                }

                // Completion waits briefly for further matches, so read the modifiers while the key goes down.
                _operation.OnPotentialMatch(_ => _heldModifierPaths = HeldModifierPaths());
            }

            _operation
                .OnComplete(_ =>
                {
                    FinishRebind();
                    completed(slot.IsChord ? CompleteChord(_heldModifierPaths) : EvaluateRebind());
                })
                .OnCancel(_ =>
                {
                    FinishRebind();
                    completed(RebindResult.Canceled);
                })
                .Start();
        }

        public bool ResolveConflict(ConflictResolution resolution)
        {
            switch (resolution)
            {
                case ConflictResolution.Replace:
                    foreach (BindingSlot conflict in _pendingConflicts)
                    {
                        for (int i = conflict.FirstPartIndex; i < conflict.FirstPartIndex + conflict.PartCount; i++)
                        {
                            conflict.Action.ApplyBindingOverride(i, string.Empty);
                        }
                    }

                    break;
                case ConflictResolution.Swap:
                    if (!TrySwap())
                    {
                        return false;
                    }

                    break;
                case ConflictResolution.Cancel:
                    RestoreOverrides(_reboundSlot, _previousOverridePaths);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            _pendingConflicts = new List<BindingSlot>();
            BindingsChanged?.Invoke();
            return true;
        }

        public bool HasConflicts()
        {
            foreach (BindingSlot slot in _slots)
            {
                if (BindingConflicts.Find(slot, _slots).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void ResetBinding(BindingSlot slot)
        {
            for (int i = slot.FirstPartIndex; i < slot.FirstPartIndex + slot.PartCount; i++)
            {
                slot.Action.RemoveBindingOverride(i);
            }

            BindingsChanged?.Invoke();
        }

        public void ResetAll()
        {
            _asset.RemoveAllBindingOverrides();
            BindingsChanged?.Invoke();
        }

        public string ExportOverrides()
        {
            return _asset.SaveBindingOverridesAsJson();
        }

        /// <summary>
        /// Writes the held modifiers into the chord whose key was just captured.
        /// The composite fixes the modifier count, so any other number of held modifiers restores the chord.
        /// </summary>
        private RebindResult CompleteChord(IReadOnlyList<string> heldModifierPaths)
        {
            if (heldModifierPaths.Count != _reboundSlot.ModifierCount)
            {
                RestoreOverrides(_reboundSlot, _previousOverridePaths);
                return RebindResult.WrongModifierCount;
            }

            for (int i = 0; i < heldModifierPaths.Count; i++)
            {
                _reboundSlot.Action.ApplyBindingOverride(_reboundSlot.FirstPartIndex + i, heldModifierPaths[i]);
            }

            return EvaluateRebind();
        }

        private static List<string> HeldModifierPaths()
        {
            Keyboard keyboard = Keyboard.current;
            List<string> paths = new List<string>(3);
            if (keyboard.ctrlKey.isPressed)
            {
                paths.Add(CTRL_PATH);
            }

            if (keyboard.shiftKey.isPressed)
            {
                paths.Add(SHIFT_PATH);
            }

            if (keyboard.altKey.isPressed)
            {
                paths.Add(ALT_PATH);
            }

            return paths;
        }

        private RebindResult EvaluateRebind()
        {
            _pendingConflicts = BindingConflicts.Find(_reboundSlot, _slots);
            if (_pendingConflicts.Count > 0)
            {
                return RebindResult.Conflict;
            }

            BindingsChanged?.Invoke();
            return RebindResult.Completed;
        }

        private bool TrySwap()
        {
            if (_pendingConflicts.Count != 1)
            {
                return false;
            }

            // Equal chords have equal part counts, so the previous keys fit the other slot part for part.
            BindingSlot other = _pendingConflicts[0];
            string[] otherOverridePaths = new string[other.PartCount];
            for (int part = 0; part < other.PartCount; part++)
            {
                otherOverridePaths[part] = other.Action.bindings[other.FirstPartIndex + part].overridePath;
                other.Action.ApplyBindingOverride(other.FirstPartIndex + part, _previousEffectivePaths[part]);
            }

            if (BindingConflicts.Find(other, _slots).Count > 0 || BindingConflicts.Find(_reboundSlot, _slots).Count > 0)
            {
                RestoreOverrides(other, otherOverridePaths);
                return false;
            }

            return true;
        }

        private static void RestoreOverrides(BindingSlot slot, string[] overridePaths)
        {
            for (int part = 0; part < overridePaths.Length; part++)
            {
                int bindingIndex = slot.FirstPartIndex + part;
                if (overridePaths[part] == null)
                {
                    slot.Action.RemoveBindingOverride(bindingIndex);
                }
                else
                {
                    slot.Action.ApplyBindingOverride(bindingIndex, overridePaths[part]);
                }
            }
        }

        private void SuspendEnabledMaps()
        {
            foreach (InputActionMap map in _asset.actionMaps)
            {
                if (map.enabled)
                {
                    _suspendedMaps.Add(map);
                    map.Disable();
                }
            }
        }

        // Button actions re-enabled while the captured key is still held do not fire until it is pressed again.
        private void FinishRebind()
        {
            _operation.Dispose();
            _operation = null;
            foreach (InputActionMap map in _suspendedMaps)
            {
                map.Enable();
            }

            _suspendedMaps.Clear();
        }
    }
}
