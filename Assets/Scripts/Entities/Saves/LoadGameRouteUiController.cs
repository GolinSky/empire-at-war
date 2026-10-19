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
        private SaveBrowserUi _ui;
        private IReadOnlyList<BattleSaveHeader> _slots = Array.Empty<BattleSaveHeader>();
        private int _modeIndex;

        public LoadGameRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            IBattleSaveRepository saves, IGameCommand game) : base(uiService, cancelRouter)
        {
            _saves = saves;
            _game = game;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SaveBrowserUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }
            _ui.SetTitle("LOAD GAME");
            _ui.SetSaveAsNewVisible(false);
            _ui.SetNameEntryVisible(false);
            SelectMode(_modeIndex);
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            _ui.Hide();
            Unfocus();
        }

        public void SelectMode(int index)
        {
            _modeIndex = index;
            _ui.SetMode(index, true);
            try
            {
                _slots = _saves.List(index == 0 ? GameMode.Skirmish : GameMode.Survival);
                var labels = new List<string>();
                var enabled = new List<bool>();
                foreach (BattleSaveHeader slot in _slots)
                {
                    labels.Add($"{slot.Slot.Name} — {slot.SavedAtUtc.ToLocalTime():g} — {TimeSpan.FromSeconds(slot.BattleTime):hh\\:mm\\:ss}{(slot.IsCompatible ? "" : " (unsupported version)")}");
                    enabled.Add(slot.IsCompatible);
                }
                _ui.SetSaves(labels, enabled);
                _ui.SetStatus(_slots.Count == 0 ? "No saves for this mode." : "");
            }
            catch (Exception exception)
            {
                _slots = Array.Empty<BattleSaveHeader>();
                _ui.SetSaves(new List<string>(), new List<bool>());
                _ui.SetStatus($"Cannot read saves: {exception.Message}");
            }
        }

        public void SelectSave(int index)
        {
            BattleSaveHeader slot = _slots[index];
            if (!slot.IsCompatible)
            {
                _ui.SetStatus("This save version is not supported.");
                return;
            }
            try { _game.LoadBattle(_saves.Load(slot.Mode, slot.Slot)); }
            catch (Exception exception) { _ui.SetStatus($"Load failed: {exception.Message}"); }
        }

        public void SaveAsNew() => throw new InvalidOperationException("The load screen cannot create saves.");
        public void ConfirmName(string name) => throw new InvalidOperationException("The load screen cannot create saves.");
        public void CancelName() => _ui.SetNameEntryVisible(false);

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void LateDispose()
        {
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }
    }
}
