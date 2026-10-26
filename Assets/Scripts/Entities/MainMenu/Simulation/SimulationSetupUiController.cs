using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationSetupUiController : UiController, ISimulationRoute, ISimulationSetupPresenter, ILateDisposable
    {
        private readonly SimulationDraft _draft;
        private readonly FactionCatalog _catalog;
        private readonly ISimulationLauncher _launcher;
        private SimulationSetupUi _ui;
        private readonly UnitTypeId[][] _choices = new UnitTypeId[2][];
        private static readonly FactionType[] FACTIONS = (FactionType[])Enum.GetValues(typeof(FactionType));
        public SimulationSetupUiController(IUiService ui, IUiCancelRouter cancel, SimulationDraft draft,
            FactionCatalog catalog, ISimulationLauncher launcher) : base(ui, cancel)
        { _draft = draft; _catalog = catalog; _launcher = launcher; }
        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SimulationSetupUi>(); _ui.AssignPresenter(this); _ui.Initialize();
            }
            if (_launcher.Current != null) _draft.Load(_launcher.Current);
            _launcher.Open(); Render(); _ui.Show(); Focus();
        }
        public void Close() { _ui.Hide(); Unfocus(); _launcher.Cancel(); }
        protected override bool HandleCancel() { Close(); return true; }
        public void StartBattle()
        {
            if (!_draft.CanStart) return;
            SimulationSetup setup = _draft.Build();
            if (_launcher.Current != null) _ui.Hide();
            Unfocus(); _launcher.Launch(setup);
        }
        public void SelectFaction(int side, int index)
        {
            if (side == 0) { _draft.FactionA = FACTIONS[index]; _draft.FleetA.Clear(); }
            else { _draft.FactionB = FACTIONS[index]; _draft.FleetB.Clear(); }
            Render();
        }
        public void AddUnit(int side, int unit, int quantity)
        {
            var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
            fleet.Add(new SimulationFleetEntry(_choices[side][unit], quantity)); Render();
        }
        public void RemoveUnit(int side, int index)
        {
            var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
            if (index < fleet.Count) fleet.RemoveAt(index);
            Render();
        }
        public void SetDistance(float value) { _draft.Distance = value; RenderValues(); }
        public void SetTimeLimit(float value) { _draft.TimeLimit = value; RenderValues(); }
        public void SetFastSpeed(bool value) => _draft.FastSpeed = value;
        public void SetUseHangars(bool value) => _draft.UseHangars = value;
        private void Render()
        {
            for (int side = 0; side < 2; side++)
            {
                FactionType faction = side == 0 ? _draft.FactionA : _draft.FactionB;
                var definition = _catalog.Get(faction);
                _choices[side] = definition.Ships.Keys.Select(UnitTypeId.Ship)
                    .Concat(definition.Squadrons.Keys.Select(UnitTypeId.Squadron)).ToArray();
                var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
                _ui.RenderSide(side, Array.IndexOf(FACTIONS, faction), _choices[side].Select(Name).ToArray(),
                    fleet.Select(entry => $"{Name(entry.Unit)} × {entry.Quantity}").ToArray());
            }
            RenderValues();
        }
        private static string Name(UnitTypeId unit) => unit.IsShip ? unit.ShipType.ToString() : unit.SquadronType.ToString();
        private void RenderValues() =>
            _ui.RenderValues(_draft.Distance, _draft.TimeLimit, _draft.FastSpeed, _draft.UseHangars, _draft.CanStart);
        public void LateDispose() { if (_ui != null) _ui.Dispose(); Unfocus(); }
    }
}
