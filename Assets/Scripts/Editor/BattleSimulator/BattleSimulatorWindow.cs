using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.BattleSimulator
{
    [EditorToolInfo("Deterministic combat and fleet balance simulation using runtime health and weapon rules.", typeof(BattleSimulatorWindow))]
    public sealed class BattleSimulatorWindow : EditorWindow
    {
        public const string MENU_PATH = "Tools/Empire At War/Game Design/Battle Simulator";
        private const string STATE_KEY = "EmpireAtWar.BattleSimulator.Setup";
        [SerializeField] private BattleSetup setup = new BattleSetup();
        private BalanceRegistration _registry;
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _warnings = new Dictionary<string, string>();
        private SimUnitFactory _factory;
        private MonteCarloRunner _runner;
        private BattleSummary _summary;
        private BattleSetup _runSetup;
        private VisualElement _setupView;
        private VisualElement _results;
        private Label _status;
        private ProgressBar _progress;
        private Button _run;
        private Button _cancel;
        private Button _copy;
        private Button _export;

        [MenuItem(MENU_PATH)]
        public static void Open() => EditorHubWindow.OpenTool(MENU_PATH);

        private void OnEnable()
        {
            if (EditorPrefs.HasKey(STATE_KEY)) setup = JsonUtility.FromJson<BattleSetup>(EditorPrefs.GetString(STATE_KEY));
        }

        private void OnDisable()
        {
            Cancel();
            Persist();
        }

        private void Persist() => EditorPrefs.SetString(STATE_KEY, JsonUtility.ToJson(setup));

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("balance-root");
            rootVisualElement.AddToClassList("sim-root");
            rootVisualElement.EnableInClassList("balance-light", !EditorGUIUtility.isProSkin);
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/BattleSimulator/BattleSimulator.uss"));
            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);
            scroll.Add(new Label("BATTLE SIMULATOR") { name = "sim-title" });
            scroll.Add(new HelpBox(BattleExport.LIMITATIONS, HelpBoxMessageType.Info));
            _status = new Label { name = "sim-status" };
            scroll.Add(_status);
            try { _registry = BalanceInventory.BuildCombat(); }
            catch (Exception exception)
            {
                _status.text = exception.Message;
                return;
            }
            _setupView = new VisualElement();
            scroll.Add(_setupView);
            BuildSetup();
            var actions = new VisualElement();
            actions.AddToClassList("sim-actions");
            scroll.Add(actions);
            _run = new Button(Run) { text = "Run", name = "sim-run" };
            actions.Add(_run);
            _cancel = new Button(Cancel) { text = "Cancel", name = "sim-cancel" };
            actions.Add(_cancel);
            _cancel.SetEnabled(false);
            _copy = new Button(() => EditorGUIUtility.systemCopyBuffer = BattleExport.Markdown(_summary, _runSetup)) { text = "Copy Markdown" };
            actions.Add(_copy);
            _export = new Button(() =>
            {
                try { _status.text = "Exported: " + BattleExport.Write(_summary, _runSetup); }
                catch (Exception exception) { _status.text = "Export failed: " + exception.Message; }
            }) { text = "Export CSV + Markdown" };
            actions.Add(_export);
            _progress = new ProgressBar { title = "Ready", highValue = 100 };
            scroll.Add(_progress);
            _results = new VisualElement();
            scroll.Add(_results);
            ShowResults();
        }

        private void BuildSetup()
        {
            _setupView.Clear();
            _setupView.Add(new BattleSimulatorSetupView(setup, _registry, _errors, _warnings, Persist));
        }

        private void Run()
        {
            if (_runner != null) return;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run the simulator outside Play Mode.");
                _runSetup = JsonUtility.FromJson<BattleSetup>(JsonUtility.ToJson(setup));
                BalanceDraft draft = setup.UseDraft ? BalanceEditorWindow.LoadSavedDraft() : new BalanceDraft();
                _factory = new SimUnitFactory(_registry, draft);
                var definitions = new Dictionary<string, SimUnitDefinition>();
                _errors.Clear();
                _warnings.Clear();
                foreach (BattleFleetEntry entry in setup.SideA.Concat(setup.SideB))
                {
                    if (definitions.ContainsKey(entry.UnitId) || _errors.ContainsKey(entry.UnitId)) continue;
                    try
                    {
                        BalanceUnit unit = _registry.Units.Single(u => u.Id == entry.UnitId);
                        SimUnitDefinition definition = _factory.Build(unit);
                        definitions.Add(entry.UnitId, definition);
                        _warnings.Add(entry.UnitId, definition.Warning);
                    }
                    catch (Exception exception) { _errors.Add(entry.UnitId, exception.Message); }
                }
                BuildSetup();
                if (_errors.Count > 0) throw new InvalidOperationException("Run blocked: fix the marked unit bindings/data or remove those units.");
                // Validate even empty/invalid fleets before subscribing to the editor update loop.
                new BattleSimulation(_runSetup, definitions, _factory.Matrix, setup.Seed, false);
                _runner = new MonteCarloRunner(_runSetup, definitions, _factory.Matrix);
                _summary = _runner.Summary;
                _results.Clear();
                _setupView.SetEnabled(false);
                _run.SetEnabled(false);
                _cancel.SetEnabled(true);
                _copy.SetEnabled(false);
                _export.SetEnabled(false);
                _status.text = "Running a read-only " + (setup.UseDraft ? "draft" : "live asset") + " snapshot…";
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Release();
                _status.text = exception.Message;
            }
        }

        private void Tick()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Cancel();
                    return;
                }
                _runner.Advance();
                _progress.value = _runner.Progress * 100;
                _progress.title = $"{_summary.Completed}/{_runSetup.Runs} seeds";
                if (!_runner.IsComplete) return;
                Release();
                ShowResults();
                _status.text = "Complete. " + _errors.Count + " selected unit errors.";
            }
            catch (Exception exception)
            {
                Cancel();
                _status.text = "Simulation failed: " + exception.Message;
            }
        }

        private void Cancel()
        {
            if (_runner == null) return;
            _runner.Cancel();
            Release();
            ShowResults();
            _status.text = "Cancelled. Only completed seeds are included.";
        }

        private void Release()
        {
            EditorApplication.update -= Tick;
            _runner = null;
            if (_factory != null)
            {
                _factory.Dispose();
                _factory = null;
            }
            if (_setupView != null) _setupView.SetEnabled(true);
            if (_run != null) _run.SetEnabled(true);
            if (_cancel != null) _cancel.SetEnabled(false);
        }

        private void ShowResults()
        {
            _results.Clear();
            if (_summary != null) _results.Add(new BattleSimulatorResultsView(_summary));
            _copy.SetEnabled(_summary != null);
            _export.SetEnabled(_summary != null);
        }
    }
}
