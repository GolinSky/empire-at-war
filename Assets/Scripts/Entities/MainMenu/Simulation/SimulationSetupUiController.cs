using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationSetupUiController : UiController, ISimulationRoute, ISimulationSetupPresenter, ILateDisposable
    {
        private readonly SimulationDraft _draft;
        private readonly FactionCatalog _catalog;
        private readonly IAssetService _assets;
        private readonly ISimulationLauncher _launcher;
        private readonly IReadOnlyList<ISimulationSetupPause> _setupPauses;
        private SimulationSetupUi _ui;
        private readonly UnitTypeId[][] _choices = new UnitTypeId[2][];
        private readonly Dictionary<UnitTypeId, ShipClass> _classes = new();
        private static readonly ShipClass[] UNIT_CLASSES = Enum.GetValues(typeof(ShipClass)).Cast<ShipClass>()
            .Where(type => type != ShipClass.Structure).OrderBy(type => type.ToString()).ToArray();
        private static readonly FactionType[] FACTIONS = (FactionType[])Enum.GetValues(typeof(FactionType));
        public SimulationSetupUiController(IUiService ui, IUiCancelRouter cancel, SimulationDraft draft,
            FactionCatalog catalog, IAssetService assets, ISimulationLauncher launcher, IReadOnlyList<ISimulationSetupPause> setupPauses) : base(ui, cancel)
        { _draft = draft; _catalog = catalog; _assets = assets; _launcher = launcher; _setupPauses = setupPauses; }
        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SimulationSetupUi>(); _ui.AssignPresenter(this);
                _ui.Initialize(UNIT_CLASSES.Select(type => type.ToString()).Prepend("All types").ToArray());
            }
            if (_launcher.Current != null) _draft.Load(_launcher.Current);
            foreach (var pause in _setupPauses) pause.Pause();
            for (int side = 0; side < 2; side++)
            {
                SelectUnitType(side, 0);
                RenderFleet(side);
            }
            RenderValues(); _ui.Show(); Focus();
        }
        public void Close()
        {
            _ui.Hide(); Unfocus();
            foreach (var pause in _setupPauses) pause.Resume();
        }
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
            SelectUnitType(side, 0);
            RenderFleet(side);
            RenderValues();
        }
        public void SelectUnitType(int side, int index)
        {
            FactionType faction = side == 0 ? _draft.FactionA : _draft.FactionB;
            FactionDefinition definition = _catalog.Get(faction);
            var roster = definition.Ships.Keys.Select(UnitTypeId.Ship)
                .Concat(definition.Squadrons.Keys.Select(UnitTypeId.Squadron));
            _choices[side] = roster.Where(unit => index == 0 || ClassOf(unit) == UNIT_CLASSES[index - 1]).ToArray();
            _ui.RenderPicker(side, Array.IndexOf(FACTIONS, faction), index,
                _choices[side].Select(unit => DataFor(definition, unit)).ToArray());
        }
        public void AddUnit(int side, int unit, int quantity)
        {
            var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
            fleet.Add(new SimulationFleetEntry(_choices[side][unit], quantity));
            RenderFleet(side);
            RenderValues();
        }
        public void RemoveUnit(int side, int index)
        {
            var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
            if (index < fleet.Count) fleet.RemoveAt(index);
            RenderFleet(side);
            RenderValues();
        }
        public void SetDistance(float value) { _draft.Distance = value; RenderValues(); }
        public void SetTimeLimit(float value) { _draft.TimeLimit = value; RenderValues(); }
        public void SetFastSpeed(bool value) => _draft.FastSpeed = value;
        public void SetUseHangars(bool value) => _draft.UseHangars = value;
        public void SetUseAbilities(bool value) => _draft.UseAbilities = value;
        private ShipClass ClassOf(UnitTypeId unit)
        {
            if (_classes.TryGetValue(unit, out ShipClass type)) return type;
            type = unit.IsShip
                ? _assets.Load<ShipData>(_assets.Load<ShipsData>(nameof(ShipsData)).GetShipDataPath(unit.ShipType)).ShipClass
                : _assets.Load<SquadronData>(unit.SquadronType + nameof(SquadronData)).ShipClass;
            _classes.Add(unit, type);
            return type;
        }
        private static FactionData DataFor(FactionDefinition definition, UnitTypeId unit) =>
            unit.IsShip ? definition.Ships[unit.ShipType] : definition.Squadrons[unit.SquadronType];
        private void RenderFleet(int side)
        {
            var fleet = side == 0 ? _draft.FleetA : _draft.FleetB;
            _ui.RenderFleet(side, fleet.Select(entry =>
            {
                FactionData data = entry.Unit.IsShip ? _catalog.GetShipFactionData(entry.Unit.ShipType)
                    : _catalog.GetSquadronFactionData(entry.Unit.SquadronType);
                return $"{data.Name} × {entry.Quantity}";
            }).ToArray());
        }
        private void RenderValues() =>
            _ui.RenderValues(_draft.Distance, _draft.TimeLimit, _draft.FastSpeed, _draft.UseHangars, _draft.CanStart, _draft.UseAbilities);
        public void LateDispose() { if (_ui != null) _ui.Dispose(); Unfocus(); }
    }
}
