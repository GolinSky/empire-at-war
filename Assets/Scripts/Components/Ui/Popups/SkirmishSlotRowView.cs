using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.MenuUi.Popups;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Ui.Popups
{
    /// <summary>
    /// One player row of the skirmish setup: who plays it, which faction and which team.
    /// Forwards every change with its row index so the popup can pass it straight to the presenter.
    /// </summary>
    public class SkirmishSlotRowView : MonoBehaviour
    {
        private const string HUMAN_OCCUPANT_LABEL = "You";

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Dropdown occupantDropdown;
        [SerializeField] private TMP_Dropdown factionDropdown;
        [SerializeField] private TMP_Dropdown teamDropdown;

        private int _rowIndex;
        private bool _isHumanRow;

        public event Action<int, int> OccupantChanged;
        public event Action<int, int> FactionChanged;
        public event Action<int, int> TeamChanged;

        public void Initialize(
            int rowIndex,
            bool isHumanRow,
            IReadOnlyList<string> aiOccupantOptions,
            IReadOnlyList<string> factionOptions,
            IReadOnlyList<string> teamOptions)
        {
            _rowIndex = rowIndex;
            _isHumanRow = isHumanRow;
            titleText.text = isHumanRow ? "COMMANDER" : $"PLAYER {rowIndex + 1}";
            SetOptions(occupantDropdown, isHumanRow ? new[] { HUMAN_OCCUPANT_LABEL } : aiOccupantOptions);
            // The human row always plays; only AI rows can be opened, closed or re-levelled.
            occupantDropdown.interactable = !isHumanRow;
            SetOptions(factionDropdown, factionOptions);
            SetOptions(teamDropdown, teamOptions);

            occupantDropdown.onValueChanged.AddListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.AddListener(HandleFactionChanged);
            teamDropdown.onValueChanged.AddListener(HandleTeamChanged);
        }

        public void Render(SkirmishSlotSetup slot)
        {
            // AI occupant options follow the SkirmishSlotOccupant order, so the enum value is the option index.
            occupantDropdown.SetValueWithoutNotify(_isHumanRow ? 0 : (int)slot.Occupant);
            factionDropdown.SetValueWithoutNotify((int)slot.Faction);
            teamDropdown.SetValueWithoutNotify(slot.Team);
            factionDropdown.interactable = slot.IsOpen;
            teamDropdown.interactable = slot.IsOpen;
            occupantDropdown.RefreshShownValue();
            factionDropdown.RefreshShownValue();
            teamDropdown.RefreshShownValue();
        }

        public void Dispose()
        {
            occupantDropdown.onValueChanged.RemoveListener(HandleOccupantChanged);
            factionDropdown.onValueChanged.RemoveListener(HandleFactionChanged);
            teamDropdown.onValueChanged.RemoveListener(HandleTeamChanged);
        }

        private static void SetOptions(TMP_Dropdown dropdown, IReadOnlyList<string> options)
        {
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }

            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }

        private void HandleOccupantChanged(int value) => OccupantChanged?.Invoke(_rowIndex, value);

        private void HandleFactionChanged(int value) => FactionChanged?.Invoke(_rowIndex, value);

        private void HandleTeamChanged(int value) => TeamChanged?.Invoke(_rowIndex, value);
    }
}
