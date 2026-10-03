using System.Collections.Generic;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>
    /// Drives interactive rebinding for the settings screen and copies every accepted change into the settings draft.
    /// </summary>
    public sealed class KeyBindingEditor
    {
        private const string SWAP_REJECTED_MESSAGE = "Swapping would create another conflict. Choose Replace or Cancel.";

        private readonly IInputBindings _bindings;
        private readonly ISettingsService _settings;

        private readonly SettingsModel _model;

        public KeyBindingEditor(IInputBindings bindings, ISettingsService settings, SettingsModel model)
        {
            _bindings = bindings;
            _settings = settings;
            _model = model;
        }

        public void Refresh()
        {
            IReadOnlyList<BindingSlot> slots = _bindings.RebindableSlots;
            KeyBindingRowState[] rows = new KeyBindingRowState[slots.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new KeyBindingRowState(slots[i].Label, _bindings.GetBindingDisplayString(slots[i]));
            }

            _model.SetBindings(rows);
        }

        public void StartRebind(int row)
        {
            BindingSlot slot = _bindings.RebindableSlots[row];
            _model.SetPrompt(new SettingsPrompt(
                kind: SettingsPromptKind.ListeningForKey,
                message: $"Press a key or mouse button for {slot.Label}.\nEsc cancels.",
                actions: new SettingsPromptAction[0]));
            _bindings.StartRebind(slot, result => HandleRebindCompleted(slot, result));
        }

        public void ResetBinding(int row)
        {
            _bindings.ResetBinding(_bindings.RebindableSlots[row]);
            SyncDraft();
        }

        public void ResetAll()
        {
            _bindings.ResetAll();
            SyncDraft();
        }

        public void ResolveConflict(ConflictResolution resolution)
        {
            if (!_bindings.ResolveConflict(resolution))
            {
                _model.SetStatus(SWAP_REJECTED_MESSAGE);
                return;
            }

            _model.SetPrompt(SettingsPrompt.None);
            SyncDraft();
        }

        private void HandleRebindCompleted(BindingSlot slot, RebindResult result)
        {
            switch (result)
            {
                case RebindResult.Completed:
                    _model.SetPrompt(SettingsPrompt.None);
                    SyncDraft();
                    break;
                case RebindResult.Conflict:
                    Refresh();
                    _model.SetPrompt(CreateConflictPrompt(slot));
                    break;
                default:
                    _model.SetPrompt(SettingsPrompt.None);
                    break;
            }
        }

        private SettingsPrompt CreateConflictPrompt(BindingSlot slot)
        {
            IReadOnlyList<BindingSlot> conflicts = _bindings.PendingConflicts;
            List<string> labels = new List<string>(conflicts.Count);
            foreach (BindingSlot conflict in conflicts)
            {
                labels.Add(conflict.Label);
            }

            List<SettingsPromptAction> actions = new List<SettingsPromptAction> { SettingsPromptAction.Replace };
            if (conflicts.Count == 1)
            {
                actions.Add(SettingsPromptAction.Swap);
            }

            actions.Add(SettingsPromptAction.Cancel);
            string key = _bindings.GetBindingDisplayString(slot);
            return new SettingsPrompt(
                kind: SettingsPromptKind.BindingConflict,
                message: $"{key} is already used by: {string.Join(", ", labels)}.\nReplace unbinds it there.",
                actions: actions);
        }

        private void SyncDraft()
        {
            _settings.Draft.Input.BindingOverridesJson = _bindings.ExportOverrides();
            Refresh();
            _model.SetDirty(_settings.IsDirty);
        }
    }
}
