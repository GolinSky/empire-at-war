using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Rebinds actions of the shared <see cref="GameInputActions"/> and applies saved overrides from settings.
    /// </summary>
    public sealed class InputBindingService : IInputBindings, ISettingsApplier, IDisposable
    {
        private const string UNBOUND_LABEL = "Unbound";
        private const string CANCEL_REBIND_PATH = "<Keyboard>/escape";
        private const string POINTER_POSITION_PATH = "<Pointer>/position";
        private const string POINTER_DELTA_PATH = "<Pointer>/delta";

        private readonly InputActionAsset _asset;
        private readonly List<BindingSlot> _slots;
        private readonly List<InputActionMap> _suspendedMaps = new List<InputActionMap>();
        private List<BindingSlot> _pendingConflicts = new List<BindingSlot>();
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private BindingSlot _reboundSlot;
        private string _previousOverridePath;
        private string _previousEffectivePath;

        public event Action BindingsChanged;

        public IReadOnlyList<BindingSlot> RebindableSlots => _slots;
        public IReadOnlyList<BindingSlot> PendingConflicts => _pendingConflicts;

        public InputBindingService(InputActionsProvider provider)
        {
            _asset = provider.Actions.asset;
            _slots = BindingSlotCatalog.Build(provider.Actions.Camera.Get(), provider.Actions.Battle.Get());
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

        public void Dispose()
        {
            if (_operation != null)
            {
                FinishRebind();
            }
        }

        public string GetBindingDisplayString(BindingSlot slot)
        {
            string display = slot.Action.GetBindingDisplayString(slot.BindingIndex);
            return string.IsNullOrEmpty(display) ? UNBOUND_LABEL : display;
        }

        public void StartRebind(BindingSlot slot, Action<RebindResult> completed)
        {
            if (_operation != null || _pendingConflicts.Count > 0)
            {
                throw new InvalidOperationException("A rebind is already in progress.");
            }

            InputBinding binding = slot.Action.bindings[slot.BindingIndex];
            _reboundSlot = slot;
            _previousOverridePath = binding.overridePath;
            _previousEffectivePath = binding.effectivePath;
            // Capture owns the keyboard: no gameplay or UI action (Escape included) may react meanwhile.
            SuspendEnabledMaps();
            _operation = slot.Action.PerformInteractiveRebinding(slot.BindingIndex)
                .WithControlsExcluding(POINTER_POSITION_PATH)
                .WithControlsExcluding(POINTER_DELTA_PATH)
                .WithCancelingThrough(CANCEL_REBIND_PATH)
                .OnComplete(_ =>
                {
                    FinishRebind();
                    completed(EvaluateRebind());
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
                        conflict.Action.ApplyBindingOverride(conflict.BindingIndex, string.Empty);
                    }

                    break;
                case ConflictResolution.Swap:
                    if (!TrySwap())
                    {
                        return false;
                    }

                    break;
                case ConflictResolution.Cancel:
                    RestoreOverride(_reboundSlot, _previousOverridePath);
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
            slot.Action.RemoveBindingOverride(slot.BindingIndex);
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

            BindingSlot other = _pendingConflicts[0];
            string otherOverridePath = other.Action.bindings[other.BindingIndex].overridePath;
            other.Action.ApplyBindingOverride(other.BindingIndex, _previousEffectivePath);
            if (BindingConflicts.Find(other, _slots).Count > 0 || BindingConflicts.Find(_reboundSlot, _slots).Count > 0)
            {
                RestoreOverride(other, otherOverridePath);
                return false;
            }

            return true;
        }

        private static void RestoreOverride(BindingSlot slot, string overridePath)
        {
            if (overridePath == null)
            {
                slot.Action.RemoveBindingOverride(slot.BindingIndex);
            }
            else
            {
                slot.Action.ApplyBindingOverride(slot.BindingIndex, overridePath);
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
