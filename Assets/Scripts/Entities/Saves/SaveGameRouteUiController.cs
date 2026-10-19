using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Saves
{
    public sealed class SaveGameRouteUiController : UiController, ISaveGameRoute,
        ISaveBrowserPresenter, IInitializable, ILateDisposable
    {
        private readonly BattleDefinition _battle;
        private readonly IBattleSaveRepository _repository;
        private readonly BattleSaveActions _saves;
        private IReadOnlyList<BattleSaveHeader> _headers = Array.Empty<BattleSaveHeader>();
        private SaveBrowserUi _ui;
        private bool _enteringName;
        private bool _saving;

        public SaveGameRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            BattleDefinition battle, IBattleSaveRepository repository, BattleSaveActions saves)
            : base(uiService, cancelRouter)
        {
            _battle = battle;
            _repository = repository;
            _saves = saves;
        }

        public void Initialize()
        {
            _ui = CreateUi<SaveBrowserUi>();
            _ui.AssignPresenter(this);
            _ui.Initialize();
            _ui.Hide();
            _saves.StatusChanged += OnSaveStatus;
        }

        public void Open()
        {
            if (!_saves.IsSupported) return;
            _ui.SetTitle($"SAVE GAME — {_battle.Mode.ToString().ToUpperInvariant()}");
            _ui.SetMode((int)_battle.Mode, false);
            _ui.SetSaveAsNewVisible(true);
            CancelName();
            _ui.SetStatus(_saving ? _saves.Status : "Choose a save to overwrite, or save as new.");
            RefreshSaves();
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            CancelName();
            _ui.Hide();
            Unfocus();
        }

        public void SelectMode(int index)
        {
            // A running battle can only save into its own mode.
        }

        public void SelectSave(int index)
        {
            if (!CanSave()) return;
            Save(_headers[index].Slot);
        }

        public void RemoveSave(int index)
        {
            if (!CanSave()) return;
            BattleSaveHeader header = _headers[index];
            try
            {
                _repository.Delete(_battle.Mode, header.Slot);
                _ui.SetStatus($"Removed {header.Slot.Name}.");
                RefreshSaves();
            }
            catch (Exception exception)
            {
                _ui.SetStatus($"Could not remove save: {exception.Message}");
            }
        }

        public void SaveAsNew()
        {
            if (!CanSave()) return;
            _enteringName = true;
            _ui.SetStatus("Enter a name for the new save.");
            _ui.SetNameEntryVisible(true);
        }

        public void ConfirmName(string name)
        {
            if (!CanSave()) return;
            try
            {
                BattleSaveSlot slot = BattleSaveSlot.Named(name);
                foreach (BattleSaveHeader header in _repository.List(_battle.Mode))
                {
                    if (string.Equals(header.Slot.Name, slot.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _ui.SetStatus("That name already exists. Choose another name or select the existing save to overwrite it.");
                        return;
                    }
                }
                Save(slot);
            }
            catch (Exception exception)
            {
                _ui.SetStatus($"Save failed: {exception.Message}");
            }
        }

        public void CancelName()
        {
            _enteringName = false;
            _ui.SetNameEntryVisible(false);
        }

        protected override bool HandleCancel()
        {
            if (_enteringName) CancelName();
            else Close();
            return true;
        }

        public void LateDispose()
        {
            _saves.StatusChanged -= OnSaveStatus;
            if (_ui != null) _ui.Dispose();
            Unfocus();
        }

        private bool CanSave()
        {
            if (!_saving && _saves.CanSave) return true;
            _ui.SetStatus("Saving is unavailable while the battle is loading, ended, or saving.");
            return false;
        }

        private void Save(BattleSaveSlot slot)
        {
            _saving = true;
            _saves.Save(slot);
        }

        private void OnSaveStatus(string status)
        {
            if (!_saving) return;
            _ui.SetStatus(status);
            if (status.StartsWith("Saving ", StringComparison.Ordinal)) return;
            _saving = false;
            if (status.StartsWith("Saved ", StringComparison.Ordinal))
            {
                CancelName();
                RefreshSaves();
            }
        }

        private void RefreshSaves()
        {
            try
            {
                _headers = _repository.List(_battle.Mode);
                var labels = new List<string>();
                var enabled = new List<bool>();
                foreach (BattleSaveHeader header in _headers)
                {
                    labels.Add($"{header.Slot.Name}    {header.SavedAtUtc.ToLocalTime():g}");
                    enabled.Add(true);
                }
                _ui.SetSaves(labels, enabled);
            }
            catch (Exception exception)
            {
                _headers = Array.Empty<BattleSaveHeader>();
                _ui.SetSaves(new List<string>(), new List<bool>());
                _ui.SetStatus($"Could not list saves: {exception.Message}");
            }
        }
    }
}
