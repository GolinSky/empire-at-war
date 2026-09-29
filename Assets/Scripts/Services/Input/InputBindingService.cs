using System;
using EmpireAtWar.Services.Settings;
using UnityEngine.InputSystem;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Rebinds actions of the shared <see cref="GameInputActions"/> and persists the overrides through settings.
    /// </summary>
    public sealed class InputBindingService : IInputBindings, IInitializable, IDisposable
    {
        private readonly InputActionAsset _asset;
        private readonly ISettingsService _settingsService;
        private InputActionRebindingExtensions.RebindingOperation _operation;

        public event Action BindingsChanged;

        public InputBindingService(InputActionsProvider provider, ISettingsService settingsService)
        {
            _asset = provider.Actions.asset;
            _settingsService = settingsService;
        }

        public void Initialize()
        {
            string overridesJson = _settingsService.LoadInputBindingOverrides();
            if (!string.IsNullOrEmpty(overridesJson))
            {
                _asset.LoadBindingOverridesFromJson(overridesJson);
            }
        }

        public void Dispose()
        {
            if (_operation != null)
            {
                _operation.Dispose();
                _operation = null;
            }
        }

        public string GetBindingDisplayString(InputAction action, int bindingIndex)
        {
            return action.GetBindingDisplayString(bindingIndex);
        }

        public void StartRebind(InputAction action, int bindingIndex, Action<RebindResult> completed)
        {
            if (_operation != null)
            {
                throw new InvalidOperationException("A rebind is already in progress.");
            }

            bool wasEnabled = action.enabled;
            string previousOverridePath = action.bindings[bindingIndex].overridePath;
            // An action must be disabled while it is being rebound.
            action.Disable();
            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(_ =>
                {
                    FinishRebind(action, wasEnabled);
                    completed(ApplyRebind(action, bindingIndex, previousOverridePath));
                })
                .OnCancel(_ =>
                {
                    FinishRebind(action, wasEnabled);
                    completed(RebindResult.Canceled);
                })
                .Start();
        }

        public void ResetBinding(InputAction action, int bindingIndex)
        {
            action.RemoveBindingOverride(bindingIndex);
            Save();
        }

        public void ResetAll()
        {
            _asset.RemoveAllBindingOverrides();
            Save();
        }

        private RebindResult ApplyRebind(InputAction action, int bindingIndex, string previousOverridePath)
        {
            if (HasConflict(action, bindingIndex))
            {
                if (string.IsNullOrEmpty(previousOverridePath))
                {
                    action.RemoveBindingOverride(bindingIndex);
                }
                else
                {
                    action.ApplyBindingOverride(bindingIndex, previousOverridePath);
                }

                return RebindResult.Conflict;
            }

            Save();
            return RebindResult.Completed;
        }

        private void FinishRebind(InputAction action, bool wasEnabled)
        {
            _operation.Dispose();
            _operation = null;
            if (wasEnabled)
            {
                action.Enable();
            }
        }

        // Two actions of one map sharing a control would fire together.
        private static bool HasConflict(InputAction action, int bindingIndex)
        {
            InputBinding rebound = action.bindings[bindingIndex];
            foreach (InputAction mapAction in action.actionMap.actions)
            {
                for (int i = 0; i < mapAction.bindings.Count; i++)
                {
                    InputBinding binding = mapAction.bindings[i];
                    bool isSameBinding = mapAction == action && i == bindingIndex;
                    if (!isSameBinding && !binding.isComposite &&
                        binding.effectivePath == rebound.effectivePath)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Save()
        {
            _settingsService.SaveInputBindingOverrides(_asset.SaveBindingOverridesAsJson());
            BindingsChanged?.Invoke();
        }
    }
}
