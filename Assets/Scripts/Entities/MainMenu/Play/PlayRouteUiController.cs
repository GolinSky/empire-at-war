using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Services.Saves;
using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    public sealed class PlayRouteUiController : UiController, IPlayRoute, IPlayPresenter, ILateDisposable
    {
        private readonly ISkirmishRoute _skirmishRoute;
        private readonly ISurvivalRoute _survivalRoute;
        private readonly ISimulationRoute _simulationRoute;
        private PlayUi _ui;
        private readonly IBattleSaveRepository _saves;
        private readonly IGameCommand _game;
        private IReadOnlyList<BattleSaveHeader> _slots;
        private int _modeIndex;
        private int _slotIndex;

        public PlayRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            ISkirmishRoute skirmishRoute, ISurvivalRoute survivalRoute, ISimulationRoute simulationRoute,
            IBattleSaveRepository saves, IGameCommand game) : base(uiService, cancelRouter)
        {
            _skirmishRoute = skirmishRoute;
            _survivalRoute = survivalRoute;
            _simulationRoute = simulationRoute;
            _saves = saves;
            _game = game;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<PlayUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }
            _ui.Show();
            SelectSaveMode(_modeIndex);
            Focus();
        }

        public void Close()
        {
            _ui.Hide();
            Unfocus();
        }

        public void OpenSkirmish() => _skirmishRoute.Open();
        public void OpenSurvival() => _survivalRoute.Open();
        public void OpenSimulation() => _simulationRoute.Open();

        public void SelectSaveMode(int index)
        {
            _modeIndex = index;
            _slotIndex = 0;
            try
            {
                _slots = _saves.List(index == 0 ? GameMode.Skirmish : GameMode.Survival);
                var labels = new List<string>();
                foreach (var slot in _slots)
                    labels.Add($"{slot.Slot.Name} — {slot.SavedAtUtc.ToLocalTime():g} — {TimeSpan.FromSeconds(slot.BattleTime):hh\\:mm\\:ss}{(slot.IsCompatible ? "" : " (unsupported version)")}");
                _ui.SetSaves(index, labels);
                SelectSave(0);
            }
            catch (Exception exception)
            {
                _slots = Array.Empty<BattleSaveHeader>();
                _ui.SetSaves(index, new List<string>());
                _ui.SetLoadState(false, $"Cannot read saves: {exception.Message}");
            }
        }

        public void SelectSave(int index)
        {
            _slotIndex = index;
            bool exists = index >= 0 && index < _slots.Count;
            _ui.SetLoadState(exists && _slots[index].IsCompatible,
                !exists ? "No saves for this mode." : _slots[index].IsCompatible ? "" : "This save version is not supported.");
        }

        public void LoadSave()
        {
            try
            {
                var slot = _slots[_slotIndex];
                _game.LoadBattle(_saves.Load(slot.Mode, slot.Slot));
            }
            catch (Exception exception) { _ui.SetLoadState(true, $"Load failed: {exception.Message}"); }
        }

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
