using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Saves
{
    public sealed class LoadGameRouteUiController : UiController, ILoadGameRoute, ISaveBrowserPresenter, ILateDisposable
    {
        private readonly IBattleSaveRepository _saves;
        private readonly IGameCommand _game;
        private readonly GameData _gameData;
        private SaveBrowserUi _ui;
        private IReadOnlyList<BattleSaveHeader> _slots = Array.Empty<BattleSaveHeader>();
        private int _modeIndex;
        private int _selectedIndex = -1;

        public LoadGameRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            IBattleSaveRepository saves, IGameCommand game, GameData gameData = null) : base(uiService, cancelRouter)
        {
            _saves = saves;
            _game = game;
            _gameData = gameData;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SaveBrowserUi>();
                _ui.AssignPresenter(this);
                _ui.ModeSelected += SelectMode;
                _ui.LoadRequested += LoadSelected;
                _ui.DeleteRequested += DeleteSelected;
                _ui.Initialize();
            }
            _ui.SetTitle("LOAD GAME");
            _ui.SetSaveAsNewVisible(false);
            _ui.SetNameEntryVisible(false);
            if (_gameData != null && !string.IsNullOrEmpty(_gameData.PendingLoadError))
            {
                string error = _gameData.PendingLoadError;
                _gameData.PendingLoadError = null;
                SelectMode(_gameData.GameMode == GameMode.Survival ? 1 : 0);
                _ui.SetStatus(error);
            }
            else
            {
                SelectMode(_modeIndex);
            }
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            _ui.Hide();
            Unfocus();
        }

        private void SelectMode(int index)
        {
            _modeIndex = index;
            _selectedIndex = -1;
            _ui.SetMode(index, true);
            try
            {
                _slots = _saves.List(index == 0 ? GameMode.Skirmish : GameMode.Survival);
                _ui.SetLoadSaves(_slots);
                _ui.SetStatus(_slots.Count == 0 ? "No saves for this mode." : "");
                if (_slots.Count > 0) SelectSave(0);
            }
            catch (Exception exception)
            {
                _slots = Array.Empty<BattleSaveHeader>();
                _selectedIndex = -1;
                _ui.SetLoadSaves(_slots);
                _ui.SetStatus($"Cannot read saves: {exception.Message}");
            }
        }

        public void SelectSave(int index)
        {
            _selectedIndex = index;
            _ui.SetLoadSelection(index, _slots[index]);
            _ui.SetStatus(_slots[index].IsCompatible ? "" : "This save version is not supported.");
        }

        private void LoadSelected()
        {
            if (_selectedIndex < 0) return;
            BattleSaveHeader slot = _slots[_selectedIndex];
            if (!slot.IsCompatible)
            {
                _ui.SetStatus("This save version is not supported.");
                return;
            }
            try { _game.LoadBattle(_saves.Load(slot.Mode, slot.Slot)); }
            catch (Exception exception) { _ui.SetStatus($"Load failed: {exception.Message}"); }
        }

        private void DeleteSelected()
        {
            if (_selectedIndex >= 0) RemoveSave(_selectedIndex);
        }

        public void RemoveSave(int index)
        {
            BattleSaveHeader slot = _slots[index];
            try
            {
                _saves.Delete(slot.Mode, slot.Slot);
                SelectMode(_modeIndex);
            }
            catch (Exception exception)
            {
                _ui.SetStatus($"Could not remove save: {exception.Message}");
            }
        }

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void LateDispose()
        {
            if (_ui != null)
            {
                _ui.ModeSelected -= SelectMode;
                _ui.LoadRequested -= LoadSelected;
                _ui.DeleteRequested -= DeleteSelected;
                _ui.Dispose();
            }
            _ui = null;
            Unfocus();
        }
    }
}
