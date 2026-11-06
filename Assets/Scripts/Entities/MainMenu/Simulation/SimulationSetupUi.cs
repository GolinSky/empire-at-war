using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationSetupUi : BaseUi
    {
        [SerializeField] private TMP_Dropdown[] factions;
        [SerializeField] private TMP_Dropdown[] unitTypes;
        [SerializeField] private TMP_Dropdown[] units;
        [SerializeField] private RectTransform[] fleetContents;
        [SerializeField] private SimulationFleetRowUi[] fleetRowTemplates;
        [SerializeField] private Slider[] quantities;
        [SerializeField] private TMP_Text[] quantityLabels;
        [SerializeField] private Slider distance;
        [SerializeField] private Slider timeLimit;
        [SerializeField] private TMP_Text distanceLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Toggle fastSpeed;
        [SerializeField] private Toggle useHangars;
        [SerializeField] private Toggle useAbilities;
        [SerializeField] private Button startButton;
        [SerializeField] private Button closeButton;
        private ISimulationSetupPresenter _presenter;
        private bool _initialized;
        private readonly List<SimulationFleetRowUi>[] _fleetRows =
            { new List<SimulationFleetRowUi>(), new List<SimulationFleetRowUi>() };
        public void AssignPresenter(ISimulationSetupPresenter presenter) => _presenter = presenter;
        public void Initialize(string[] typeNames)
        {
            if (_initialized) return;
            foreach (var dropdown in factions) Fill(dropdown, Enum.GetNames(typeof(FactionType)));
            foreach (var dropdown in unitTypes) Fill(dropdown, typeNames);
            factions[0].onValueChanged.AddListener(FactionA); factions[1].onValueChanged.AddListener(FactionB);
            unitTypes[0].onValueChanged.AddListener(TypeA); unitTypes[1].onValueChanged.AddListener(TypeB);
            units[0].onValueChanged.AddListener(AddA); units[1].onValueChanged.AddListener(AddB);
            quantities[0].onValueChanged.AddListener(QuantityA); quantities[1].onValueChanged.AddListener(QuantityB);
            distance.onValueChanged.AddListener(_presenter.SetDistance);
            timeLimit.onValueChanged.AddListener(_presenter.SetTimeLimit);
            fastSpeed.onValueChanged.AddListener(_presenter.SetFastSpeed);
            useHangars.onValueChanged.AddListener(_presenter.SetUseHangars);
            useAbilities.onValueChanged.AddListener(_presenter.SetUseAbilities);
            startButton.onClick.AddListener(_presenter.StartBattle); closeButton.onClick.AddListener(_presenter.Close);
            QuantityA(quantities[0].value); QuantityB(quantities[1].value); _initialized = true;
        }
        public void RenderPicker(int side, int faction, int type, IReadOnlyList<FactionData> choices)
        {
            factions[side].SetValueWithoutNotify(faction);
            unitTypes[side].SetValueWithoutNotify(type);
            TMP_Dropdown dropdown = units[side];
            dropdown.ClearOptions();
            foreach (FactionData choice in choices)
                dropdown.options.Add(new TMP_Dropdown.OptionData(choice.Name, choice.Icon, Color.white));
            ((TMP_Text)dropdown.placeholder).text = choices.Count == 0 ? "No matching units" : "+ ADD UNIT";
            dropdown.SetValueWithoutNotify(-1);
            dropdown.RefreshShownValue();
            dropdown.interactable = choices.Count > 0;
        }
        public void RenderFleet(int side, string[] fleet)
        {
            foreach (var row in _fleetRows[side])
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            _fleetRows[side].Clear();
            for (int index = 0; index < fleet.Length; index++)
            {
                int entry = index;
                var row = Instantiate(fleetRowTemplates[side], fleetContents[side]);
                row.Initialize(fleet[index], () => _presenter.RemoveUnit(side, entry));
                row.gameObject.SetActive(true);
                _fleetRows[side].Add(row);
            }
        }
        public void RenderValues(float separation, float seconds, bool fast, bool hangars, bool canStart, bool abilities = true)
        {
            distance.SetValueWithoutNotify(separation); timeLimit.SetValueWithoutNotify(seconds);
            distanceLabel.text = $"Separation: {separation:0} m"; timeLabel.text = $"Time limit: {seconds:0} s";
            fastSpeed.SetIsOnWithoutNotify(fast); useHangars.SetIsOnWithoutNotify(hangars);
            useAbilities.SetIsOnWithoutNotify(abilities);
            startButton.interactable = canStart;
        }
        private void FactionA(int index) => _presenter.SelectFaction(0, index);
        private void FactionB(int index) => _presenter.SelectFaction(1, index);
        private void TypeA(int index) => _presenter.SelectUnitType(0, index);
        private void TypeB(int index) => _presenter.SelectUnitType(1, index);
        private void AddA(int index) => AddUnit(0, index);
        private void AddB(int index) => AddUnit(1, index);
        private void AddUnit(int side, int index)
        {
            _presenter.AddUnit(side, index, (int)quantities[side].value);
            units[side].SetValueWithoutNotify(-1);
        }
        private void QuantityA(float value) => quantityLabels[0].text = $"Quantity: {value:0}";
        private void QuantityB(float value) => quantityLabels[1].text = $"Quantity: {value:0}";
        private static void Fill(TMP_Dropdown dropdown, IEnumerable<string> values)
        { dropdown.ClearOptions(); dropdown.AddOptions(new List<string>(values)); }
        public void Dispose()
        {
            if (!_initialized) return;
            factions[0].onValueChanged.RemoveListener(FactionA); factions[1].onValueChanged.RemoveListener(FactionB);
            unitTypes[0].onValueChanged.RemoveListener(TypeA); unitTypes[1].onValueChanged.RemoveListener(TypeB);
            units[0].onValueChanged.RemoveListener(AddA); units[1].onValueChanged.RemoveListener(AddB);
            quantities[0].onValueChanged.RemoveListener(QuantityA); quantities[1].onValueChanged.RemoveListener(QuantityB);
            distance.onValueChanged.RemoveListener(_presenter.SetDistance); timeLimit.onValueChanged.RemoveListener(_presenter.SetTimeLimit);
            fastSpeed.onValueChanged.RemoveListener(_presenter.SetFastSpeed);
            useHangars.onValueChanged.RemoveListener(_presenter.SetUseHangars);
            useAbilities.onValueChanged.RemoveListener(_presenter.SetUseAbilities);
            startButton.onClick.RemoveListener(_presenter.StartBattle); closeButton.onClick.RemoveListener(_presenter.Close);
            _initialized = false;
        }
        private void OnDestroy() => Dispose();
    }
}
